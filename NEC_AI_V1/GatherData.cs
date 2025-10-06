using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NEC_AI_V1
{
    internal class GatherData
    {
        // Main method to get formatted element info with all relevant parameters
        public string GetElementInfoWithParameters(Element element)
        {
            if (element == null) return "";

            string elementInfo = $"  {element.Category?.Name ?? "Unknown"} - {element.Name}";

            //get family name
            if (element is FamilyInstance famInst)
            {
                string familyName = famInst.Symbol.FamilyName;
                // Only add family name if it's different from element name
                if (!string.IsNullOrEmpty(familyName) && familyName != element.Name)
                {
                    elementInfo += $" (Family: {familyName})";
                }
            }
            // Get location
            if (element.Location is LocationPoint locPoint)
            {
                var pt = locPoint.Point;
                elementInfo += $": Position ({pt.X:F1}, {pt.Y:F1}, {pt.Z:F1})";
            }

            // Get ALL relevant parameters
            var relevantParams = GetRelevantParameters(element);
            if (relevantParams.Count > 0)
            {
                elementInfo += ", " + string.Join(", ", relevantParams.Select(kvp => $"{kvp.Key}: {kvp.Value:F1}"));
            }

            return elementInfo;
        }

        // Generic parameter extraction method
        private Dictionary<string, double> GetRelevantParameters(Element element)
        {
            var results = new Dictionary<string, double>();

            // List of parameter names that are useful for spatial reasoning
            var relevantParamNames = new[]
            {
                BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM,
                BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM,
                BuiltInParameter.FAMILY_HEIGHT_PARAM,
                BuiltInParameter.FAMILY_WIDTH_PARAM,
                //BuiltInParameter.FAMILY_DEPTH_PARAM,
                BuiltInParameter.WINDOW_HEIGHT,
                BuiltInParameter.DOOR_HEIGHT,
                BuiltInParameter.INSTANCE_ELEVATION_PARAM
            };

            foreach (var paramId in relevantParamNames)
            {
                Parameter param = element.get_Parameter(paramId);
                if (param != null && param.HasValue && param.StorageType == StorageType.Double)
                {
                    string friendlyName = LabelUtils.GetLabelFor(paramId);
                    results[friendlyName] = param.AsDouble();
                }
            }

            // Also check the element's type/symbol for parameters
            if (element is FamilyInstance famInst)
            {
                foreach (var paramId in relevantParamNames)
                {
                    Parameter typeParam = famInst.Symbol.get_Parameter(paramId);
                    if (typeParam != null && typeParam.HasValue && typeParam.StorageType == StorageType.Double)
                    {
                        string friendlyName = LabelUtils.GetLabelFor(paramId);
                        if (!results.ContainsKey(friendlyName))
                        {
                            results[friendlyName] = typeParam.AsDouble();
                        }
                    }
                }
            }

            return results;
        }
    }
}