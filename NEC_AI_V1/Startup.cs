using Autodesk.Revit.UI;
using System.Threading.Tasks;

namespace NEC_AI_V1
{
    public class Startup : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            // Check for updates on startup
            //Task.Run(() => Updates.CheckForUpdates());
            //TaskDialog.Show("update has been show","sucess");
            try
            {
                Updates.CheckForUpdates();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Startup Error", ex.Message);
            }


            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}