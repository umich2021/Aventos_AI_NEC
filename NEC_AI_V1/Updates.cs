using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEC_AI_V1
{
    internal class Updates
    {
        public static async void CheckForUpdates()
        {

            try
            {
                using (var client = new System.Net.WebClient())
                {
                    // Check version on your server
                    string latestVersion = await client.DownloadStringTaskAsync("https://raw.githubusercontent.com/umich2021/Aventos_AI_NEC/master/NEC_AI_V1/Version/Version.txt");
                    //string currentVersion = GetCurrentVersion();
                    string currentVersion = "1.0.1";//technically we're suppose to automatically look up version.txt, but i'm just going to do it later
                    //TaskDialog.Show("current version is updated", $"this is working latest version is {latestVersion}");
                    //TaskDialog.Show("current version is updated", $"this is working latest version is {currentVersion}");


                    if (latestVersion.Trim() != currentVersion)
                    {
                        var result = TaskDialog.Show("Update Available",
                            $"A new version ({latestVersion}) of Aventos AI is available!\n\n" +
                            $"You're currently using version {currentVersion}.\n\n" +
                            $"Visit aventos.dev/download to update.",
                            TaskDialogCommonButtons.Yes);

                        if (result == TaskDialogResult.Yes)
                        {
                            //sends the user to our site to download, I think this is too annoying
                            //System.Diagnostics.Process.Start("https://aventos.dev/download");
                            
                        }
                    }
                    else
                    {
                        //TaskDialog.Show("current version is updated", $"current version is {currentVersion}");
                    }
                }
            }
            catch (Exception ex)
            {
                //TaskDialog.Show("Update Check Failed", $"Error: {ex.Message}");
                // Silently fail if offline or server unreachable
            }
        }
        private static string GetCurrentVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "NEC_AI_V1.version.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                TaskDialog.Show("result of version", $"versioning is {reader.ReadToEnd().Trim()}");
                return reader.ReadToEnd().Trim();
            }
        }
        
    }
}
