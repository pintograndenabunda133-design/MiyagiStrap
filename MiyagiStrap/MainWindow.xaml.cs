using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MiyagiStrap
{
    public partial class MainWindow : Window
    {
        private readonly string _launchUri;
        private readonly AppSettings _settings;
        private bool _ready;

        public MainWindow(string launchUri)
        {
            InitializeComponent();
            _launchUri = launchUri;
            _settings = AppSettings.Load();
            LoadSettingsIntoUi();

            Loaded += async (_, _) =>
            {
                _ready = true;
                RestartPetals();

                if (Installer.NeedsInstall && _launchUri == null)
                {
                    ShowInstaller();
                    return;
                }

                NavHome.IsChecked = true;
                if (_launchUri != null)
                    await LaunchAsync(_launchUri);
            };
        }

        private void LoadSettingsIntoUi()
        {
            FpsSlider.Value = _settings.FpsLimit;
            SetChecked(RendererPanel, _settings.Renderer);
            SetChecked(MsaaPanel, _settings.Msaa.ToString());
            CustomBox.Text = _settings.CustomFlags ?? "";
            PetalsCheck.IsChecked = _settings.PetalsEnabled;
            PetalSlider.Value = _settings.PetalCount;
            PetalText.Text = _settings.PetalCount.ToString();
            AboutText.Text = "Instalado em: " + Installer.InstallDir;
        }

        private static void SetChecked(StackPanel panel, string tag)
        {
            foreach (var rb in panel.Children.OfType<RadioButton>())
                rb.IsChecked = (string)rb.Tag == tag;
        }

        private static string GetChecked(StackPanel panel, string fallback)
        {
            foreach (var rb in panel.Children.OfType<RadioButton>())
            {
                if (rb.IsChecked == true) return (string)rb.Tag;
            }
            return fallback;
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void MinBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (!_ready) return;
            string tag = (string)((RadioButton)sender).Tag;
            PanelHome.Visibility = tag == "home" ? Visibility.Visible : Visibility.Collapsed;
            PanelFlags.Visibility = tag == "flags" ? Visibility.Visible : Visibility.Collapsed;
            PanelLook.Visibility = tag == "look" ? Visibility.Visible : Visibility.Collapsed;
            PanelAbout.Visibility = tag == "about" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FpsSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FpsText == null) return;
            int fps = (int)FpsSlider.Value;
            FpsText.Text = fps == 0 ? "Padrão" : fps + " FPS";
        }

        private void SaveFlags_Click(object sender, RoutedEventArgs e)
        {
            string custom = CustomBox.Text.Trim();
            int customCount = 0;
            if (custom.Length > 0)
            {
                if (!RobloxInstaller.TryParseCustomFlags(custom, out var parsed, out string error))
                {
                    FlagsStatus.Text = error;
                    return;
                }
                customCount = parsed.Count;
            }

            _settings.FpsLimit = (int)FpsSlider.Value;
            _settings.Renderer = GetChecked(RendererPanel, "auto");
            _settings.Msaa = int.Parse(GetChecked(MsaaPanel, "-1"));
            _settings.CustomFlags = custom;
            _settings.Save();
            FlagsStatus.Text = "Salvo! " + (customCount > 0 ? customCount + " flags personalizadas. " : "") +
                               "Vale na próxima vez que abrir o Roblox.";
        }

        private void Look_Changed(object sender, RoutedEventArgs e)
        {
            if (!_ready) return;
            _settings.PetalsEnabled = PetalsCheck.IsChecked == true;
            _settings.PetalCount = (int)PetalSlider.Value;
            PetalText.Text = _settings.PetalCount.ToString();
            _settings.Save();
            RestartPetals();
        }

        private void RestartPetals()
        {
            PetalCanvas.Children.Clear();
            if (_settings.PetalsEnabled && _settings.PetalCount > 0)
                PetalRain.Start(PetalCanvas, _settings.PetalCount);
        }

        private async void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchAsync(null);
        }

        private async Task LaunchAsync(string uri)
        {
            LaunchButton.IsEnabled = false;
            try
            {
                var progress = new Progress<(string Text, int Percent)>(p =>
                {
                    StatusText.Text = p.Text;
                    ProgressBar.Value = p.Percent;
                });

                RobloxInstaller.RegisterProtocol();
                string exe = await RobloxInstaller.EnsureLatestAsync(progress);
                string flagsFile = RobloxInstaller.ApplyFastFlags(exe, _settings, out int appliedCount);
                if (flagsFile != null)
                    StatusText.Text = appliedCount + " flag(s) aplicadas. Abrindo o Roblox...";
                else
                    StatusText.Text = "Nenhuma flag configurada. Abrindo o Roblox...";

                RobloxInstaller.Launch(exe, uri);
                await Task.Delay(1500);

                if (uri != null)
                {
                    Close();
                }
                else
                {
                    StatusText.Text = "Roblox aberto!";
                    LaunchButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Erro: " + ex.Message;
                LaunchButton.IsEnabled = true;
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(Installer.InstallDir))
                Process.Start("explorer.exe", Installer.InstallDir);
            else
                MessageBox.Show("A pasta ainda não existe. Abre o Roblox uma vez primeiro.", "Miyagi Strap");
        }

        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            var answer = MessageBox.Show("Quer mesmo desinstalar o Miyagi Strap?", "Miyagi Strap",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            Installer.Uninstall();
            Application.Current.Shutdown();
        }

        private void ShowInstaller()
        {
            Sidebar.Visibility = Visibility.Collapsed;
            ContentArea.Visibility = Visibility.Collapsed;
            PanelInstall.Visibility = Visibility.Visible;
            InstallPathText.Text = Installer.InstallDir;
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            InstallButton.IsEnabled = false;
            try
            {
                Installer.Install(ChkDesktop.IsChecked == true, ChkStartMenu.IsChecked == true);
                Process.Start(new ProcessStartInfo(Installer.InstalledExe) { UseShellExecute = true });
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                InstallStatus.Text = "Erro ao instalar: " + ex.Message;
                InstallButton.IsEnabled = true;
            }
        }
    }
}
