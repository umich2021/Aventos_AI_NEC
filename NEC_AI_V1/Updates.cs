using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
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
                    string currentVersion = "1.0.0"; // Update this with each release
                    TaskDialog.Show("current version is updated", $"this is working latest version is {latestVersion}");

                    if (latestVersion.Trim() != currentVersion)
                    {
                        var result = TaskDialog.Show("Update Available",
                            $"A new version ({latestVersion}) of Aventos AI is available!\n\n" +
                            $"You're currently using version {currentVersion}.\n\n" +
                            $"Visit aventos.dev/download to update.",
                            TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                        if (result == TaskDialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start("https://aventos.dev/download");
                        }
                    }
                    else
                    {
                        TaskDialog.Show("current version is updated", $"current version is {currentVersion}");
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Update Check Failed", $"Error: {ex.Message}");
                // Silently fail if offline or server unreachable
            }
        }
    }
}
