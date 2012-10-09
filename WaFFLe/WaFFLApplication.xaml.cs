using System.Windows;
using System.Collections.Generic;

namespace WaFFL.Evaluation
{
    /// <summary>
    /// Interaction logic for WaFFLApplication.xaml
    /// </summary>
    public partial class WaFFLApplication : Application
    {
        /// <summary>
        /// Occurs when the application starts up.
        /// </summary>
        /// <param name="e">The StartupEventArgs that contain the event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //WaFFLOutput.DisplayAll();
        }
    }
}
