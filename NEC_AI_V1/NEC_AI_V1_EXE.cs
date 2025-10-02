using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;

namespace NEC_AI_V1
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GatherBIMInfoCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Create collector
                var collector = new SpaceRoomCollector(doc);

                // Get all spaces and rooms
                var allSpaces = collector.GetAllSpacesAndRooms();

                // Process each space
                foreach (var space in allSpaces)
                {
                    AnalyzeSpace(space, doc);
                }

                TaskDialog.Show("Success", $"Processed {allSpaces.Count} spaces/rooms");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        public class OutletData
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public string Name { get; set; }
        }

        private void AnalyzeSpace(SpaceRoomInfo space, Document doc)
        {
            string spaceInfo = $"=== {space.Name} ({space.Number}) ===\n";
            spaceInfo += $"Area: {space.Area:F2} sq ft | Volume: {space.Volume:F2} cu ft\n";
            spaceInfo += $"Level: {space.LevelName} | Department: {space.Department}\n";
            spaceInfo += $"Occupancy: {space.OccupancyType}\n\n";

            // Get detailed element information - SHOW EVERY SINGLE ITEM
            spaceInfo += "ALL ELEMENTS FOUND:\n";
            if (space.ContainedElements.Count == 0)
            {
                spaceInfo += "  No elements found in this space.\n";
            }
            else
            {
                foreach (var elementId in space.ContainedElements)
                {
                    var element = doc.GetElement(elementId);
                    if (element != null)
                    {
                        spaceInfo += GetElementDetails(element) + "\n";
                    }
                    else
                    {
                        spaceInfo += $"• ERROR: Element ID {elementId} not found\n";
                    }
                }
            }

            // Show custom parameters
            if (space.CustomParameters.Count > 0)
            {
                spaceInfo += "\nCUSTOM PARAMETERS:\n";
                foreach (var param in space.CustomParameters)
                {
                    spaceInfo += $"  {param.Key}: {param.Value}\n";
                }
            }

            spaceInfo += $"\nTOTAL ELEMENTS: {space.ContainedElements.Count}\n";
            
            //Added outlet placement logic based on room type
            if (IsBedroomSpace(space.Name))
            {
                var outlets = GenerateBedroomOutlets(space);
                //PATH TO FAMILY
                // Add this to your AnalyzeSpace method right before outlet placement:
                string roomDebug = $"Room: {space.Name}\n";
                roomDebug += $"Elements in this room:\n";
                foreach (var elementId in space.ContainedElements)
                {
                    var element = doc.GetElement(elementId);
                    if (element?.Location is LocationPoint locPoint)
                    {
                        roomDebug += $"  {element.Name}: ({locPoint.Point.X:F1}, {locPoint.Point.Y:F1})\n";
                    }
                }
                TaskDialog.Show("Room Boundary Debug", roomDebug);
                //PlaceElectricalOutlets(doc, outlets, space, null, "Table-Coffee", "24\" x 24\" x 24\"");
                PlaceElectricalOutlets(doc, outlets, space, null, "Face_outlet", "Face_outlet");

            }

            TaskDialog.Show($"DEVELOPMENT: {space.Name}", spaceInfo);
        }
        private FamilySymbol LoadAndGetFamilySymbol(Document doc, string familyPath, string familyName, string typeName)
        {
            try
            {
                // Load the family from file (only if path provided)
                if (!string.IsNullOrEmpty(familyPath))
                {
                    if (!doc.LoadFamily(familyPath))
                    {
                        TaskDialog.Show("Error", $"Failed to load family from: {familyPath}");
                        return null;
                    }
                }

                // Find the specific symbol/type
                var collector = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol));

                foreach (FamilySymbol symbol in collector)
                {
                    // CHECK FOR MATCHING FAMILY AND TYPE - THIS WAS MISSING!
                    if (symbol.Family.Name == familyName && symbol.Name == typeName)
                    {
                        // Activate symbol if not active
                        if (!symbol.IsActive)
                        {
                            using (Transaction activateTransaction = new Transaction(doc, "Activate Symbol"))
                            {
                                activateTransaction.Start();
                                symbol.Activate();
                                doc.Regenerate();
                                activateTransaction.Commit();
                            }
                        }

                        // RETURN THE FOUND SYMBOL - THIS WAS MISSING!
                        return symbol;
                    }
                }

                TaskDialog.Show("Error", $"Symbol '{typeName}' not found in family '{familyName}'");
                return null;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error loading family: {ex.Message}");
                return null;
            }
        }
        // CHANGE 5: Added main outlet placement method with transaction handling
        // --- REPLACEMENT: PlaceElectricalOutlets ---
        private bool PlaceElectricalOutlets(
            Document doc,
            List<OutletData> outletData,
            SpaceRoomInfo space,
            string familyPath = null,
            string familyName = "Electrical Outlet",
            string typeName = "Duplex Outlet")
        {
            // find symbol
            FamilySymbol outletSymbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(s => s.Family.Name == familyName && s.Name == typeName);

            if (outletSymbol == null)
            {
                TaskDialog.Show("Error", $"Could not find family '{familyName}' with type '{typeName}'");
                return false;
            }

            // activate if needed
            if (!outletSymbol.IsActive)
            {
                using (var t = new Transaction(doc, "Activate Outlet Symbol"))
                {
                    t.Start();
                    outletSymbol.Activate();
                    doc.Regenerate();
                    t.Commit();
                }
            }

            // resolve level
            Level roomLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == space.LevelName);

            if (roomLevel == null)
            {
                TaskDialog.Show("Error", $"Level '{space.LevelName}' not found.");
                return false;
            }

            int placed = 0;

            using (var trans = new Transaction(doc, "Place Electrical Outlets"))
            {
                trans.Start();
                XYZ roomCenter = CalculateRoomCenter(space, doc);

                try
                {
                    foreach (var od in outletData)
                    {
                        XYZ desiredPoint = new XYZ(od.X, od.Y, od.Z);
                        Wall hostWall = GetNearestWall(doc, desiredPoint);


                        // nearest wall and projection
                        if (hostWall == null)
                        {
                            TaskDialog.Show("Warning", $"No wall found near outlet {od.Name}");
                            continue;
                        }

                        XYZ wallPoint = ProjectPointToWall(desiredPoint, hostWall);
                        //XYZ interiorDir = GetInteriorDirection(hostWall, space, doc);
                        XYZ finalPoint = new XYZ(wallPoint.X, wallPoint.Y, desiredPoint.Z); // + interiorDir * 1; // small offset
                        finalPoint = desiredPoint;
                        // DEBUG INFORMATION
                        string debugMsg = $"Outlet {od.Name} Debug:\n";
                        debugMsg += $"Desired Point: ({od.X:F1}, {od.Y:F1}, {od.Z:F1})\n";
                        debugMsg += $"Wall Point: ({wallPoint.X:F1}, {wallPoint.Y:F1}, {wallPoint.Z:F1})\n";
                       // debugMsg += $"Interior Direction: ({interiorDir.X:F2}, {interiorDir.Y:F2}, {interiorDir.Z:F2})\n";
                        debugMsg += $"Final Point: ({finalPoint.X:F1}, {finalPoint.Y:F1}, {finalPoint.Z:F1})\n";
                        TaskDialog.Show($"Outlet {od.Name} Debug", debugMsg);
                        FamilyInstance fi = null;


                        // Use wall hosted-based placement directly
                        Reference faceRef = GetInteriorFaceReference(hostWall, roomCenter);

                        //below code puts the face reference on the outside, all things should be in exterior
                        //faceRef = HostObjectUtils.GetSideFaces(hostWall, ShellLayerType.Exterior).FirstOrDefault();
                        if (faceRef != null)
                        {
                            FamilyInstance outletInstance = null;
                            try
                            {
                                // For face-based placement, project point onto the face
                                GeometryObject geoObj = hostWall.GetGeometryObjectFromReference(faceRef);
                                Face face = geoObj as Face;

                                if (face != null)
                                {
                                    IntersectionResult intResult = face.Project(wallPoint);
                                    if (intResult != null)
                                    {
                                        UV uv = intResult.UVPoint;
                                        XYZ facePoint = face.Evaluate(uv);
                                        Transform faceTransform = face.ComputeDerivatives(uv);

                                        // Create face-based instance
                                        outletInstance = doc.Create.NewFamilyInstance(
                                            faceRef,
                                            facePoint,
                                            faceTransform.BasisX,  // U direction on face
                                            outletSymbol
                                        );
                                    }
                                }

                               // Ensure level param is set when possible
                                Parameter lvlParam = outletInstance.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                                if (lvlParam != null && !lvlParam.IsReadOnly)
                                {
                                    lvlParam.Set(roomLevel.Id);

                                    TaskDialog.Show("Placement Method", "SUCCESS: Wall-hosted placement used");
                                }
                            }

                            catch (Exception ex)
                            {
                                TaskDialog.Show("Wall-hosted Failed", $"Wall-hosted placement failed: {ex.Message}");
                                // fallback: non-hosted placement
                                //won't ever be used btw cuz receptcales are hosted placed elements
                                try
                                {
                                    outletInstance = doc.Create.NewFamilyInstance(finalPoint, outletSymbol, roomLevel, StructuralType.NonStructural);
                                }
                                catch
                                {
                                    outletInstance = null;
                                }
                            }
                        }
                        else
                        {
                            // no face ref, fallback to non-hosted
                            try
                            {
                                fi = doc.Create.NewFamilyInstance(finalPoint, outletSymbol, roomLevel, StructuralType.NonStructural);
                            }
                            catch
                            {
                                fi = null;
                            }
                        }

                        if (fi == null) continue;

                        // optional mark
                        if (!string.IsNullOrEmpty(od.Name))
                        {
                            var mark = fi.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                            if (mark != null && !mark.IsReadOnly) mark.Set(od.Name);
                        }

                        // enforce facing into the room (geometric rotation fallback)
                        //FixFacingToRoom(doc, fi, space);

                        placed++;
                    }

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    TaskDialog.Show("Error", $"Error placing outlets: {ex.Message}");
                    return false;
                }
            }

            TaskDialog.Show("Success", $"Successfully placed {placed} outlets");
            return true;
        }

        //// --- HELPER 2: FixFacingToRoom (rotation fallback only) ---
        //private void FixFacingToRoom(Document doc, FamilyInstance fi, SpaceRoomInfo space)
        //{
        //    if (fi == null) return;

        //    var lp = fi.Location as LocationPoint;
        //    if (lp == null) return;

        //    XYZ p = lp.Point;
        //    XYZ roomCenter = CalculateRoomCenter(space, doc);
        //    if (roomCenter.IsZeroLength()) return;

        //    XYZ toRoom = (roomCenter - p);
        //    if (toRoom.IsZeroLength()) return;
        //    toRoom = toRoom.Normalize();

        //    XYZ facing = fi.FacingOrientation;
        //    if (facing == null || facing.IsZeroLength()) return;
        //    facing = facing.Normalize();

        //    double angle = facing.AngleTo(toRoom);
        //    TaskDialog.Show("Rotation Debug",
        //       $"Facing: ({facing.X:F2}, {facing.Y:F2})\n" +
        //       $"To Room: ({toRoom.X:F2}, {toRoom.Y:F2})\n" +
        //       $"Angle: {angle:F2} radians\n" +
        //       $"Needs rotation: {angle > Math.PI / 2.0}");

        //    // Calculate exact rotation needed to face the room
        //    try
        //    {
        //        // Calculate the angle we need to rotate to face the room
        //        double currentAngle = Math.Atan2(facing.Y, facing.X);
        //        double targetAngle = Math.Atan2(toRoom.Y, toRoom.X);
        //        double rotationAngle = targetAngle - currentAngle;

        //        // Normalize angle to [-π, π] range
        //        while (rotationAngle > Math.PI) rotationAngle -= 2 * Math.PI;
        //        while (rotationAngle < -Math.PI) rotationAngle += 2 * Math.PI;

        //        // Only rotate if the angle difference is significant (more than 10 degrees)
        //        if (Math.Abs(rotationAngle) > Math.PI / 18.0) // 10 degrees
        //        {
        //            Line axis = Line.CreateBound(p, p + XYZ.BasisZ);
        //            ElementTransformUtils.RotateElement(doc, fi.Id, axis, rotationAngle);

        //            TaskDialog.Show("Rotation Applied",
        //                $"Rotated {rotationAngle * 180 / Math.PI:F1} degrees");
        //        }
        //    }
        //    catch
        //    {
        //        // swallow rotation errors; leave instance as placed
        //    }
        //}
        // --- HELPER 1: GetInteriorFaceReference ---
        private Reference GetInteriorFaceReference(Wall wall, XYZ roomCenter)
        {
            TaskDialog.Show("Face Method Called", "GetInteriorFaceReference is being called");
            try
            {
                // Get all interior faces
                IList<Reference> interiorRefs = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Interior);

                //debug how many interior faces found
                TaskDialog.Show("Face Debug", $"Interior faces found: {interiorRefs?.Count ?? 0}");

                if (interiorRefs == null || interiorRefs.Count == 0)
                {
                    // Fallback to exterior if no interior faces
                    IList<Reference> exteriorRefs = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Exterior);
                    return exteriorRefs?.FirstOrDefault();
                }

                // Single face wall - use it
                if (interiorRefs.Count == 1)
                {
                    //return interiorRefs[0];

                    Reference interiorRef = interiorRefs[0];

                    // Check if this "interior" face is actually facing the room
                    GeometryObject geoObj = wall.GetGeometryObjectFromReference(interiorRef);
                    if (geoObj is Face face)
                    {
                        XYZ faceNormal = face.ComputeNormal(UV.Zero);
                        XYZ wallCenter = ((LocationCurve)wall.Location).Curve.Evaluate(0.5, true);
                        XYZ toRoom = (roomCenter - wallCenter).Normalize();

                        // If normal points AWAY from room, use exterior face instead
                        if (faceNormal.DotProduct(toRoom) < 0)
                        {
                            TaskDialog.Show("Wall Orientation", $"Wall {wall.Name} interior faces away from room, using exterior");
                            IList<Reference> exteriorRefs = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Exterior);
                            return exteriorRefs?.FirstOrDefault();
                        }
                    }

                    return interiorRefs[0];
                }

                // COMPOUND WALL - Multiple interior faces
                // Find the face closest to the room center
                Reference bestFace = null;
                double closestDistance = double.MaxValue;

                foreach (Reference faceRef in interiorRefs)
                {
                    try
                    {
                        // Get the actual face geometry
                        GeometryObject geoObj = wall.GetGeometryObjectFromReference(faceRef);
                        if (geoObj is Face face)
                        {
                            // Get center point of this face
                            BoundingBoxUV bbox = face.GetBoundingBox();
                            UV centerUV = new UV(
                                (bbox.Min.U + bbox.Max.U) / 2,
                                (bbox.Min.V + bbox.Max.V) / 2
                            );
                            XYZ faceCenter = face.Evaluate(centerUV);

                            // Calculate distance to room center
                            double distance = faceCenter.DistanceTo(roomCenter);

                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                bestFace = faceRef;
                            }
                        }
                    }
                    catch
                    {
                        // Skip problematic faces
                        continue;
                    }
                }

                // Debug info for compound walls
                TaskDialog.Show("Compound Wall Debug",
                    $"Wall has {interiorRefs.Count} interior faces\n" +
                    $"Selected face at distance: {closestDistance:F2}");

                return bestFace ?? interiorRefs[0]; // Fallback to first face if calculation fails
            }
            catch
            {
                return null;
            }
        
        }

        // CHANGE 6: Added helper methods for room type detection and outlet generation
        private bool IsBedroomSpace(string spaceName)
        {
            string name = spaceName.ToLower();
            return name.Contains("br") || name.Contains("bedroom") || name.Contains("bed");
        }

        private List<OutletData> GenerateBedroomOutlets(SpaceRoomInfo space)
        {
            // Use coordinates closer to the actual room center (-9.1, 11.6)
            return new List<OutletData>
            {
                new OutletData { X = -13, Y = 0, Z = 1.5, Name = "OUT_01" },
                new OutletData { X = 2, Y = -16.3, Z = 1.5, Name = "OUT_02" },
                new OutletData { X = 5, Y = 3.5, Z = 1.5, Name = "OUT_03" },
                new OutletData { X = 5, Y = -4, Z = 1.5, Name = "OUT_04" },
                //new OutletData { X = -9.0, Y = 9.0, Z = 1.5, Name = "OUT_05" },
                //new OutletData { X = -8.5, Y = 15.0, Z = 1.5, Name = "OUT_06" }
            };
        }
        private string GetElementDetails(Element element)
        {
            string details = $"• {element.Category?.Name ?? "No Category"}";

            // Get family and type info (for appliance identification)
            if (element is FamilyInstance famInst)
            {
                details += $"\n  Family: {famInst.Symbol.FamilyName}";
                details += $"\n  Type: {famInst.Symbol.Name}";
            }

            // Get location
            if (element.Location is LocationPoint locPoint)
            {
                var pt = locPoint.Point;
                details += $"\n  Location: ({pt.X:F1}, {pt.Y:F1}, {pt.Z:F1})";
            }

            // Check for appliance types
            if (element is FamilyInstance famInst2)
            {
                string familyName = famInst2.Symbol.FamilyName.ToLower();
                string typeName = famInst2.Symbol.Name.ToLower();
                if (IsKitchenAppliance(familyName, typeName))
                {
                    details += "\n  >>> KITCHEN APPLIANCE DETECTED <<<";
                }
            }

            return details;
        }
        private bool IsKitchenAppliance(string familyName, string typeName)
        {
            string[] appliances = { "refrigerator", "fridge", "stove", "oven", "dishwasher",
                                   "microwave", "disposal", "range", "cooktop", "freezer" };

            string combined = (familyName + " " + typeName).ToLower();
            return appliances.Any(appliance => combined.Contains(appliance));
        }
        // ADD THIS METHOD
        private Wall GetNearestWall(Document doc, XYZ point)
        {
            var wallCollector = new FilteredElementCollector(doc).OfClass(typeof(Wall));
            Wall nearestWall = null;
            double minDistance = double.MaxValue;

            foreach (Wall wall in wallCollector)
            {
                LocationCurve locationCurve = wall.Location as LocationCurve;
                if (locationCurve != null)
                {
                    Curve curve = locationCurve.Curve;
                    XYZ closestPoint = curve.Project(point).XYZPoint;
                    double distance = point.DistanceTo(closestPoint);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestWall = wall;
                    }
                }
            }
            return nearestWall;
        }
        private XYZ GetInteriorDirection(Wall wall, SpaceRoomInfo space, Document doc)
        {
            LocationCurve wallLocation = wall.Location as LocationCurve;
            Curve wallCurve = wallLocation.Curve;

            // Get wall direction vector
            XYZ wallDirection = (wallCurve.GetEndPoint(1) - wallCurve.GetEndPoint(0)).Normalize();

            // Get both possible normal directions (perpendicular to wall)
            XYZ normal1 = XYZ.BasisZ.CrossProduct(wallDirection).Normalize();
            XYZ normal2 = normal1.Negate();

            // Test point on wall
            XYZ wallPoint = wallCurve.Evaluate(0.5, true);

            // Test both directions - see which one points toward room elements
            XYZ roomCenter = CalculateRoomCenter(space, doc);

            XYZ testPoint1 = wallPoint + (normal1 * 1.0); // 1 foot in direction 1
            XYZ testPoint2 = wallPoint + (normal2 * 1.0); // 1 foot in direction 2

            // Choose direction that gets closer to room center
            double dist1 = testPoint1.DistanceTo(roomCenter);
            double dist2 = testPoint2.DistanceTo(roomCenter);

            XYZ interiorDirection = (dist1 < dist2) ? normal1 : normal2;

            // DEBUG
            string debugMsg = $"Wall Normal Test:\n";
            debugMsg += $"Wall Point: ({wallPoint.X:F1}, {wallPoint.Y:F1})\n";
            debugMsg += $"Room Center: ({roomCenter.X:F1}, {roomCenter.Y:F1})\n";
            debugMsg += $"Normal1: ({normal1.X:F2}, {normal1.Y:F2}) - Distance: {dist1:F2}\n";
            debugMsg += $"Normal2: ({normal2.X:F2}, {normal2.Y:F2}) - Distance: {dist2:F2}\n";
            debugMsg += $"Chosen: ({interiorDirection.X:F2}, {interiorDirection.Y:F2})";
            TaskDialog.Show("Wall Normal Debug", debugMsg);

            return interiorDirection;
        }

        private XYZ CalculateRoomCenter(SpaceRoomInfo space, Document doc)
        {
            XYZ totalPosition = XYZ.Zero;
            int count = 0;
            string debugInfo = "Room Center Calculation:\n";

            foreach (var elementId in space.ContainedElements)
            {
                Element element = doc.GetElement(elementId);
                if (element?.Location is LocationPoint locPoint)
                {
                    totalPosition = totalPosition.Add(locPoint.Point);
                    count++;
                    debugInfo += $"Element: {element.Name} at ({locPoint.Point.X:F1}, {locPoint.Point.Y:F1})\n";
                }
            }

            XYZ roomCenter = count > 0 ? totalPosition.Divide(count) : XYZ.Zero;
            debugInfo += $"Final Room Center: ({roomCenter.X:F1}, {roomCenter.Y:F1}, {roomCenter.Z:F1})\n";
            debugInfo += $"Total elements used: {count}";

            TaskDialog.Show("Room Center Debug", debugInfo);
            return roomCenter;
        }
        // ADD THIS METHOD TOO
        private XYZ ProjectPointToWall(XYZ point, Wall wall)
        {
            LocationCurve locationCurve = wall.Location as LocationCurve;
            if (locationCurve != null)
            {
                Curve curve = locationCurve.Curve;
                IntersectionResult result = curve.Project(point);
                return result.XYZPoint;
            }
            return point;
        }
    }
}