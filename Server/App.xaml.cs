using System.Threading;
using System.Windows;
using norsu.ass.Server.Properties;

namespace norsu.ass.Server
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            awooo.Context = SynchronizationContext.Current;
            awooo.IsRunning = true;
            Network.Server.Instance.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Settings.Default.Save();
            Network.Server.Instance.Stop();
            base.OnExit(e);
        }
    }
}
