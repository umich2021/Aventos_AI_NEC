using Autodesk.Revit.DB;

namespace NEC_AI_V1.UI
{
    public static class OutletFamilyPaths
    {
        public delegate FamilySymbol LoadAndGetFamilySymbolDelegate(Document doc, string path, string familyName, string typeName);

        public static void LoadAllFamilies(Document doc, LoadAndGetFamilySymbolDelegate loadMethod)
        {
            // Load Regular outlet
            loadMethod(doc,
                @"C:\Users\jimso\Desktop\FaceOutlets\Face_outlet.rfa",
                "Face_outlet",
                "Regular");

            // Load AFCI outlet
            loadMethod(doc,
                @"C:\Users\jimso\Desktop\FaceOutlets\Face_outlet_AFCI.rfa",
                "Face_outlet_AFCI",
                "Regular");

            // Load GFCI outlet
            loadMethod(doc,
                @"C:\Users\jimso\Desktop\FaceOutlets\Face_outlet_GFCI.rfa",
                "Face_outlet_GFCI",
                "Regular");

            // Load AFCI_GFCI outlet
            loadMethod(doc,
                @"C:\Users\jimso\Desktop\FaceOutlets\Face_outlet_AFCI_GFCI.rfa",
                "Face_outlet_AFCI_GFCI",
                "Regular");
        }

        // Individual paths
        public static string RegularPath = @"C:\Users\jimso\Desktop\FaceOutlets\Face_outlet.rfa";
        public static string RegularName = "Face_outlet";
        public static string RegularType = "Regular";

        public static string AFCIPath = @"C:\Users\jimso\Desktop\FaceOutlets\Face_outlet_AFCI.rfa";
        public static string AFCIName = "Face_outlet_AFCI";
        public static string AFCIType = "Regular";

        public static string GFCIPath = @"C:\Users\jimso\Desktop\FaceOutlets\Face_outlet_GFCI.rfa";
        public static string GFCIName = "Face_outlet_GFCI";
        public static string GFCIType = "Regular";

        public static string AFCI_GFCIPath = @"C:\Users\jimso\Desktop\FaceOutlets\Face_outlet_AFCI_GFCI.rfa";
        public static string AFCI_GFCIName = "Face_outlet_AFCI_GFCI";
        public static string AFCI_GFCIType = "Regular";
    }
}