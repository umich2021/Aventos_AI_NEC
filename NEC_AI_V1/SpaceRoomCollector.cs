using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;


namespace NEC_AI_V1
{
    public class SpaceRoomInfo
    {
        public ElementId Id { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
        public double Area { get; set; }
        public double Volume { get; set; }
        public string Department { get; set; }
        public string OccupancyType { get; set; }
        public Level Level { get; set; }
        public string LevelName { get; set; }
        public List<ElementId> ContainedElements { get; set; } = new List<ElementId>();
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
    }

    public class SpaceRoomCollector
    {
        private readonly Document _doc;

        public SpaceRoomCollector(Document document)
        {
            _doc = document;
        }
        public List<ElementId> GetDoorsForRoom(Room room)
        {
            var doorIds = new List<ElementId>();

            // Get all doors in the document
            var doorCollector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType();
            //// Get room boundary segments
            //var boundaries = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
            //if (boundaries == null || boundaries.Count == 0)
            //    return doorIds;
            //int totalDoors = doorCollector.GetElementCount();
            //string debugInfo = $"Total doors in project: {totalDoors}\n";
            //debugInfo += $"Looking for room: '{room.Number}' - '{room.Name}'\n\n";

            // Get room's bounding box
            String debugInfo = "";
            BoundingBoxXYZ roomBBox = room.get_BoundingBox(null);
            if (roomBBox == null)
            {
                debugInfo += "ERROR: Room has no bounding box\n";
                TaskDialog.Show("Door Collection Debug", debugInfo);
                return doorIds;
            }

            debugInfo += $"Room BBox: ({roomBBox.Min.X:F1}, {roomBBox.Min.Y:F1}) to ({roomBBox.Max.X:F1}, {roomBBox.Max.Y:F1})\n\n";

            foreach (FamilyInstance door in doorCollector)
            {
                if (door.Location is LocationPoint doorLoc)
                {
                    XYZ doorPoint = doorLoc.Point;
                    debugInfo += $"Door '{door.Name}' at ({doorPoint.X:F1}, {doorPoint.Y:F1}, {doorPoint.Z:F1})\n";

                    // Expand bbox by 2 feet to catch doors on walls
                    double buffer = 2.0;
                    bool isNearRoom = doorPoint.X >= (roomBBox.Min.X - buffer) &&
                                     doorPoint.X <= (roomBBox.Max.X + buffer) &&
                                     doorPoint.Y >= (roomBBox.Min.Y - buffer) &&
                                     doorPoint.Y <= (roomBBox.Max.Y + buffer);

                    if (isNearRoom)
                    {
                        doorIds.Add(door.Id);
                        debugInfo += "  ✓ ADDED\n";
                    }
                    else
                    {
                        debugInfo += "  ✗ Outside range\n";
                    }
                }
            }

            debugInfo += $"\nFound {doorIds.Count} doors";
            //TaskDialog.Show("Door Collection Debug", debugInfo);

            return doorIds;
        //}
        //    foreach (FamilyInstance door in doorCollector)
        //    {
        //        // Access FromRoom and ToRoom by parameter name (they're not built-in parameters)
        //        Parameter fromRoom = door.LookupParameter("From Room");
        //        Parameter toRoom = door.LookupParameter("To Room");

        //        // Compare by room number
        //        string roomNumber = room.Number;
        //        string roomName = room.Name;

        //        string fromValue = fromRoom?.AsString() ?? "NULL";
        //        string toValue = toRoom?.AsString() ?? "NULL";
        //        debugInfo += $"Door '{door.Name}': From='{fromValue}', To='{toValue}'\n";

        //        // Check if door is associated with this room
        //        bool matchesFromRoom = fromRoom != null &&
        //            (fromRoom.AsString() == roomNumber || fromRoom.AsString() == roomName);
        //        bool matchesToRoom = toRoom != null &&
        //            (toRoom.AsString() == roomNumber || toRoom.AsString() == roomName);

        //        if (matchesFromRoom || matchesToRoom)
        //        {
        //            doorIds.Add(door.Id);
        //            debugInfo += "  ✓ MATCHED!\n";
        //        }
        //    }
        //    debugInfo += $"\nFound {doorIds.Count} doors for this room";
        //    TaskDialog.Show("Door Collection Debug", debugInfo);
        //    return doorIds;
        }

        public List<SpaceRoomInfo> GetAllRooms()
        {
            var rooms = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>()
                .Where(r => r.Area > 0)
                .ToList();

            return rooms.Select(room => new SpaceRoomInfo
            {
                Id = room.Id,
                Name = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "",
                Number = room.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "",
                Area = room.get_Parameter(BuiltInParameter.ROOM_AREA)?.AsDouble() ?? 0,
                Volume = room.get_Parameter(BuiltInParameter.ROOM_VOLUME)?.AsDouble() ?? 0,
                Department = room.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT)?.AsString() ?? "",
                OccupancyType = room.get_Parameter(BuiltInParameter.ROOM_OCCUPANCY)?.AsString() ?? "",
                Level = room.Level,
                LevelName = room.Level?.Name ?? "",
                ContainedElements = GetElementsInRoom(room),
                CustomParameters = GetCustomParameters(room)
            }).ToList();
        }

        public List<SpaceRoomInfo> GetAllSpaces()
        {
            var spaces = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .WhereElementIsNotElementType()
                .Cast<Space>()
                .Where(s => s.Area > 0)
                .ToList();

            return spaces.Select(space => new SpaceRoomInfo
            {
                Id = space.Id,
                Name = space.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "",
                Number = space.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "",
                Area = space.get_Parameter(BuiltInParameter.ROOM_AREA)?.AsDouble() ?? 0,
                Volume = space.get_Parameter(BuiltInParameter.ROOM_VOLUME)?.AsDouble() ?? 0,
                Department = space.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT)?.AsString() ?? "",
                OccupancyType = space.get_Parameter(BuiltInParameter.ROOM_OCCUPANCY)?.AsString() ?? "",
                Level = space.Level,
                LevelName = space.Level?.Name ?? "",
                ContainedElements = GetElementsInSpace(space),
                CustomParameters = GetCustomParameters(space)
            }).ToList();
        }

        private List<ElementId> GetElementsInRoom(Room room)
        {
            var elements = new List<ElementId>();

            // Get all family instances
            var familyInstances = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(fi => fi.Room?.Id == room.Id)
                .Select(fi => fi.Id)
                .ToList();

            elements.AddRange(familyInstances);

            // Get MEP equipment
            var mepEquipment = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_ElectricalEquipment)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(eq => eq.Room?.Id == room.Id)
                .Select(eq => eq.Id)
                .ToList();

            elements.AddRange(mepEquipment);

            return elements;
        }

        private List<ElementId> GetElementsInSpace(Space space)
        {
            var elements = new List<ElementId>();

            // Get all family instances in space
            var familyInstances = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(fi => fi.Space?.Id == space.Id)
                .Select(fi => fi.Id)
                .ToList();

            elements.AddRange(familyInstances);

            return elements;
        }

        private Dictionary<string, object> GetCustomParameters(Element element)
        {
            var customParams = new Dictionary<string, object>();

            foreach (Parameter param in element.Parameters)
            {
                if (param.IsReadOnly || !param.HasValue) continue;

                var paramName = param.Definition.Name;
                object value = null;

                switch (param.StorageType)
                {
                    case StorageType.String:
                        value = param.AsString();
                        break;
                    case StorageType.Integer:
                        value = param.AsInteger();
                        break;
                    case StorageType.Double:
                        value = param.AsDouble();
                        break;
                    case StorageType.ElementId:
                        value = param.AsElementId();
                        break;
                }

                if (value != null)
                    customParams[paramName] = value;
            }

            return customParams;
        }

        public List<SpaceRoomInfo> GetAllSpacesAndRooms()
        {
            var allSpaces = new List<SpaceRoomInfo>();
            allSpaces.AddRange(GetAllRooms());
            allSpaces.AddRange(GetAllSpaces());
            return allSpaces;
        }
    }
}