using System.Windows;

namespace MiyagiStrap
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string launchUri = null;
            foreach (string arg in e.Args)
            {
                if (arg == "--uninstall")
                {
                    var answer = MessageBox.Show("Quer mesmo desinstalar o Miyagi Strap?", "Miyagi Strap",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (answer == MessageBoxResult.Yes)
                    {
                        Installer.Uninstall();
                        MessageBox.Show("Miyagi Strap removido.", "Miyagi Strap");
                    }
                    Shutdown();
                    return;
                }

                if (arg.StartsWith("roblox-player:") || arg.StartsWith("roblox:"))
                    launchUri = arg;
            }

            new MainWindow(launchUri).Show();
        }
    }
}
