using Autodesk.Revit.DB;
using System;
using System.IO;
using System.Reflection;

namespace NEC_AI_V1.UI
{
    public static class OutletFamilyLoader
    {
        public delegate FamilySymbol LoadAndGetFamilySymbolDelegate(Document doc, string path, string familyName, string typeName);
        // File names
        private const string RegularFile = "Face_outlet.rfa";
        private const string AFCIFile = "Face_outlet_AFCI.rfa";
        private const string GFCIFile = "Face_outlet_GFCI.rfa";
        private const string AFCI_GFCIFile = "Face_outlet_AFCI_GFCI.rfa";
        private static string GetFaceOutletPath(string fileName)
        {
            string dllPath = Assembly.GetExecutingAssembly().Location;
            string dllDirectory = Path.GetDirectoryName(dllPath);
            string combinedPath = Path.Combine(dllDirectory, "FaceOutlets", fileName);
            return combinedPath;
        }
        public static void LoadAllFamilies(Document doc, LoadAndGetFamilySymbolDelegate loadMethod)
        {
            //retrieves relative path
            string dllPath = Assembly.GetExecutingAssembly().Location;
            string dllDirectory = Path.GetDirectoryName(dllPath);
            string faceOutletPath = Path.Combine(dllDirectory, "FaceOutlets", "Face_outlet.rfa");
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
        public static string RegularPath = GetFaceOutletPath(RegularFile);
        public static string RegularName = "Face_outlet";
        public static string RegularType = "Regular";

        public static string AFCIPath = GetFaceOutletPath(AFCIFile);
        public static string AFCIName = "Face_outlet_AFCI";
        public static string AFCIType = "Regular";

        public static string GFCIPath = GetFaceOutletPath(GFCIFile);
        public static string GFCIName = "Face_outlet_GFCI";
        public static string GFCIType = "Regular";

        public static string AFCI_GFCIPath = GetFaceOutletPath(AFCI_GFCIFile);
        public static string AFCI_GFCIName = "Face_outlet_AFCI_GFCI";
        public static string AFCI_GFCIType = "Regular";
    }
}