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
            
            // CHANGE 3: Added outlet placement logic based on room type
            if (IsBedroomSpace(space.Name))
            {
                var outlets = GenerateBedroomOutlets(space);
                //PATH TO FAMILY

                //PlaceElectricalOutlets(doc, outlets, space, null, "Table-Coffee", "24\" x 24\" x 24\"");
                PlaceElectricalOutlets(doc, outlets, space, null, "Outlet-Single", "Single");

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
        private bool PlaceElectricalOutlets(Document doc, List<OutletData> outletData, SpaceRoomInfo space, string familyPath = null,
                          string familyName = "Electrical Outlet", string typeName = "Duplex Outlet")
        {
            FamilySymbol outletSymbol = null;

            // Find the symbol first
            var collector = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol));

            foreach (FamilySymbol symbol in collector)
            {
                if (symbol.Family.Name == familyName && symbol.Name == typeName)
                {
                    outletSymbol = symbol;
                    break;
                }
            }

            if (outletSymbol == null)
            {
                TaskDialog.Show("Error", $"Could not find family '{familyName}' with type '{typeName}'");
                return false;
            }

            // FIRST TRANSACTION: Activate symbol if needed
            if (!outletSymbol.IsActive)
            {
                using (Transaction activateTransaction = new Transaction(doc, "Activate Symbol"))
                {
                    activateTransaction.Start();
                    outletSymbol.Activate();
                    doc.Regenerate();
                    activateTransaction.Commit();
                }
            }

            // SECOND TRANSACTION: Place outlets
            using (Transaction trans = new Transaction(doc, "Place Electrical Outlets"))
            {
                trans.Start();
                try
                {
                    Level roomLevel = new FilteredElementCollector(doc)
        .OfClass(typeof(Level))
        .Cast<Level>()
        .FirstOrDefault(l => l.Name == space.LevelName);

                    var placedOutlets = new List<FamilyInstance>();
                    foreach (var outlet in outletData)
                    {
                        XYZ locationPoint = new XYZ(outlet.X, outlet.Y, outlet.Z);

                        // Find nearest wall to the outlet location
                        Wall hostWall = GetNearestWall(doc, locationPoint);

                        if (hostWall != null)
                        {
                            XYZ wallPoint = ProjectPointToWall(locationPoint, hostWall);

                            // Get direction toward room interior
                            XYZ interiorDirection = GetInteriorDirection(hostWall, space, doc);
                            XYZ finalPoint = wallPoint + (interiorDirection * 0.1); // 1.2" offset
                            finalPoint = new XYZ(finalPoint.X, finalPoint.Y, locationPoint.Z);

                            //FamilyInstance outletInstance = doc.Create.NewFamilyInstance(
                            //    finalPoint,
                            //    outletSymbol,
                            //    roomLevel,  // Assign to room's level
                            //    StructuralType.NonStructural
                            //);
                            FamilyInstance outletInstance;

                            // If the family is wall-hosted, use the overload with host
                            if (outletSymbol.Family.FamilyPlacementType == FamilyPlacementType.OneLevelBasedHosted)
                            {
                                outletInstance = doc.Create.NewFamilyInstance(
                                    finalPoint,
                                    outletSymbol,
                                    hostWall,
                                    roomLevel,
                                    StructuralType.NonStructural
                                );
                            =
                                // Rotate the outlet to face the room interior
                                XYZ wallDir = (hostWall.Orientation).Normalize(); // Wall outward normal
                                XYZ facingDir = GetInteriorDirection(hostWall, space, doc); // Already have this helper

                                // Compute rotation axis (vertical axis through placement point)
                                Line axis = Line.CreateBound(finalPoint, finalPoint + XYZ.BasisZ);

                                double angle = wallDir.AngleTo(facingDir);

                                // Make sure the rotation goes in the correct direction
                                if (wallDir.CrossProduct(facingDir).Z < 0)
                                    angle = -angle;

                                ElementTransformUtils.RotateElement(doc, outletInstance.Id, axis, angle);
                            }
                            else
                            {
                                // Non-hosted (e.g., furniture-type test families)
                                outletInstance = doc.Create.NewFamilyInstance(
                                    finalPoint,
                                    outletSymbol,
                                    roomLevel,
                                    StructuralType.NonStructural
                                );
                            }


                            // Set outlet name/mark if provided
                            if (!string.IsNullOrEmpty(outlet.Name))
                            {
                                Parameter markParam = outletInstance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                                if (markParam != null && !markParam.IsReadOnly)
                                {
                                    markParam.Set(outlet.Name);
                                }
                            }

                            placedOutlets.Add(outletInstance);
                        }
                        else
                        {
                            TaskDialog.Show("Warning", $"No wall found near outlet {outlet.Name}");
                        }
                    }

                    trans.Commit();
                    TaskDialog.Show("Success", $"Successfully placed {placedOutlets.Count} outlets");
                    return true;
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    TaskDialog.Show("Error", $"Error placing outlets: {ex.Message}");
                    return false;
                }
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
            // Simple outlet generation based on room - in real implementation, 
            // you'd calculate based on room geometry and furniture location
            return new List<OutletData>
            {
                new OutletData { X = 0, Y = 0, Z = 0, Name = "OUT_00" },
                new OutletData { X = 1.0, Y = 10.0, Z = 1.5, Name = "OUT_01" },
                new OutletData { X = 5.0, Y = 10.0, Z = 1.5, Name = "OUT_02" },
                new OutletData { X = 10.0, Y = 15.0, Z = 1.5, Name = "OUT_03" },
                new OutletData { X = 15.0, Y = 12.0, Z = 1.5, Name = "OUT_04" },
                new OutletData { X = 23.117118845, Y = 13.257541451, Z = 3.758005167, Name = "OUT_05" },
                new OutletData { X = 23.117118845, Y = 9.962508272, Z = 0.738663456, Name = "OUT_06" }
            };
        }
        //RANDOM NOTES
        //No need for name, just familyname as name is often dimensions
        //sometimes type will say the name but most times it just is the dimensions
        //let's get category 
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
            // Calculate room center from furniture/elements
            XYZ roomCenter = CalculateRoomCenter(space, doc);

            // Get wall centerline point
            LocationCurve wallLocation = wall.Location as LocationCurve;
            XYZ wallPoint = wallLocation.Curve.Evaluate(0.5, true); // Midpoint

            // Direction from wall toward room center
            XYZ directionToRoom = (roomCenter - wallPoint).Normalize();

            return directionToRoom;
        }

        private XYZ CalculateRoomCenter(SpaceRoomInfo space, Document doc)
        {
            XYZ totalPosition = XYZ.Zero;
            int count = 0;

            foreach (var elementId in space.ContainedElements)
            {
                Element element = doc.GetElement(elementId);
                if (element?.Location is LocationPoint locPoint)
                {
                    totalPosition = totalPosition.Add(locPoint.Point);
                    count++;
                }
            }

            return count > 0 ? totalPosition.Divide(count) : XYZ.Zero;
        }
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