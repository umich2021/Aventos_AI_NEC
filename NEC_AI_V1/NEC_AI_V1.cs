using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
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