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
        public Result OnStartup(UIControlledApplication application)
        {
            // Check for updates (async, non-blocking)
            Task.Run(() => CheckForUpdates());

            // Rest of your startup code...
            return Result.Succeeded;
        }

        private async void CheckForUpdates()
        {
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    // Check version on your server
                    string latestVersion = await client.DownloadStringTaskAsync("https://aventos.dev/version.txt");
                    string currentVersion = "1.0.0"; // Update this with each release

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
                }
            }
            catch
            {
                // Silently fail if offline or server unreachable
            }
        }
    }
}
