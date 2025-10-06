using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace NEC_AI_V1
{
    public class ApiHelper
    {
        private readonly string _baseUrl;

        public ApiHelper(string baseUrl = "http://localhost:5001")
        {
            _baseUrl = baseUrl;
        }

        public async Task<string> GetOutletDataFromAPI(string roomInfo, string userPreferences = "")
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Create JSON manually without Newtonsoft
                    string jsonContent = $"{{\"raw_text\": \"{EscapeJsonString(roomInfo + "\\n" + userPreferences)}\"}}";
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(
                        $"{_baseUrl}/api/v1/process-bim",
                        content
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    return $"Error: {response.StatusCode}";
                }
                catch (Exception ex)
                {
                    return $"Error calling API: {ex.Message}";
                }
            }
        }
        
        private string EscapeJsonString(string str)
        {
            return str.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t");
        }
    }
}