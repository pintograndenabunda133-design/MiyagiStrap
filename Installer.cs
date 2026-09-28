using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace MiyagiStrap
{
    public static class Installer
    {
        private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\MiyagiStrap";

        public static readonly string InstallDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiyagiStrap");

        public static readonly string InstalledExe = Path.Combine(InstallDir, "MiyagiStrap.exe");

        public static bool IsSingleFile => string.IsNullOrEmpty(Assembly.GetExecutingAssembly().Location);

        public static bool NeedsInstall => IsSingleFile &&
            !string.Equals(Environment.ProcessPath, InstalledExe, StringComparison.OrdinalIgnoreCase);

        private static string DesktopLink => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Miyagi Strap.lnk");

        private static string StartMenuLink => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Miyagi Strap.lnk");

        public static void Install(bool desktopShortcut, bool startMenuShortcut)
        {
            Directory.CreateDirectory(InstallDir);
            File.Copy(Environment.ProcessPath, InstalledExe, true);

            if (desktopShortcut) CreateShortcut(DesktopLink, InstalledExe);
            if (startMenuShortcut) CreateShortcut(StartMenuLink, InstalledExe);

            using (var key = Registry.CurrentUser.CreateSubKey(UninstallKey))
            {
                key.SetValue("DisplayName", "Miyagi Strap");
                key.SetValue("DisplayVersion", "1.0.0");
                key.SetValue("Publisher", "Miyagi");
                key.SetValue("DisplayIcon", InstalledExe);
                key.SetValue("InstallLocation", InstallDir);
                key.SetValue("UninstallString", "\"" + InstalledExe + "\" --uninstall");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }

            RobloxInstaller.RegisterProtocol(InstalledExe);
        }

        public static void Uninstall()
        {
            TryDelete(DesktopLink);
            TryDelete(StartMenuLink);

            try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); } catch { }

            RemoveProtocolIfOurs("roblox-player");
            RemoveProtocolIfOurs("roblox");

            var psi = new ProcessStartInfo("cmd.exe",
                "/c ping 127.0.0.1 -n 3 > nul & rmdir /s /q \"" + InstallDir + "\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
        }

        private static void CreateShortcut(string linkPath, string target)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(shellType);
            dynamic shortcut = shell.CreateShortcut(linkPath);
            shortcut.TargetPath = target;
            shortcut.WorkingDirectory = Path.GetDirectoryName(target);
            shortcut.Description = "Miyagi Strap";
            shortcut.IconLocation = target + ",0";
            shortcut.Save();
        }

        private static void RemoveProtocolIfOurs(string scheme)
        {
            try
            {
                string command = null;
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + scheme + @"\shell\open\command"))
                {
                    command = key?.GetValue("") as string;
                }

                if (command != null && command.IndexOf(InstallDir, StringComparison.OrdinalIgnoreCase) >= 0)
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + scheme, false);
            }
            catch { }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
