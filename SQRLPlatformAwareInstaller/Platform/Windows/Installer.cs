using GitHubApi;
using Microsoft.Win32;
using Serilog;
using SQRLPlatformAwareInstaller.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQRLPlatformAwareInstaller.Platform.Windows
{
    class Installer : IInstaller
    {
        public async Task Install(string archiveFilePath, string installPath, string versionTag)
        {
            await Task.Run(() =>
            {
                Log.Information($"Installing on Windows to {installPath}");
                Inventory.Instance.Load();

                // Extract installation archive
                Log.Information($"Extracting main installation archive");
                Utils.ExtractZipFile(archiveFilePath, string.Empty, installPath);
                Inventory.Instance.AddDirectory(installPath);

                try
                {
                    Log.Information("Copying installer into installation location (for auto update)");
                    File.Copy(Process.GetCurrentProcess().MainModule.FileName, 
                        Path.Combine(installPath, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)), false);
                }
                catch (Exception fc)
                {
                    Log.Warning($"File copy exception while copying installer! {fc}");
                }

                // Create registry keys for sqrl:// scheme registration
                Log.Information("Creating registry keys for sqrl:// protocol scheme");
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl"))
                {
                    Inventory.Instance.AddRegistryKey(key.ToString());
                    key.SetValue(string.Empty, "URL:SQRL Protocol");
                    key.SetValue("URL Protocol", $"", RegistryValueKind.String);
                }
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl\DefaultIcon"))
                {
                    key.SetValue("", $"{(GetClientExePath(installPath))},0", RegistryValueKind.String);
                }
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl\shell\open\command"))
                {
                    key.SetValue("", $"\"{(GetClientExePath(installPath))}\" \"%1\"", RegistryValueKind.String);
                }

                // Create uninstall registry entries
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SQRL OSS Client"))
                {
                    Inventory.Instance.AddRegistryKey(key.ToString());
                    key.SetValue("DisplayName", "SQRL Open Source Client");
                    key.SetValue("DisplayVersion", versionTag);
                    key.SetValue("DisplayIcon", $"{GetClientExePath(installPath)},0");
                    key.SetValue("UninstallString", $"\"{GetInstallerExePath(installPath)}\" -uninstall");
                    key.SetValue("Publisher", "SQRL Developers");
                    key.SetValue("URLInfoAbout", "https://github.com/sqrldev/SQRLDotNetClient");
                    key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                    key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    key.SetValue("InstallLocation", installPath);
                }

                // Create Desktop Shortcut
                Log.Information("Create Windows desktop shortcut");
                string shortcutLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SQRL OSS Client.lnk");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"$SourceFileLocation = \"{GetClientExePath(installPath)}\"; ");
                sb.AppendLine($"$ShortcutLocation = \"{shortcutLocation}\"; ");
                sb.AppendLine("$WScriptShell = New-Object -ComObject WScript.Shell; ");
                sb.AppendLine($"$Shortcut = $WScriptShell.CreateShortcut($ShortcutLocation); ");
                sb.AppendLine($"$Shortcut.TargetPath = $SourceFileLocation; ");
                sb.AppendLine($"$Shortcut.IconLocation  = \"{GetClientExePath(installPath)}\"; ");
                sb.AppendLine($"$Shortcut.WorkingDirectory  = \"{Path.GetDirectoryName(GetClientExePath(installPath))}\"; ");
                sb.AppendLine($"$Shortcut.Save(); ");
                var tempFile = Path.GetTempFileName().Replace(".tmp", ".ps1");
                File.WriteAllText(tempFile, sb.ToString());

                Process process = new Process();
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.FileName = "powershell";
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.Arguments = $"-File {tempFile}";
                process.Start();

                Inventory.Instance.AddFile(shortcutLocation);
                Inventory.Instance.Save();
            });
        }

        public string GetClientExePath(string installPath)
        {
            return Path.Combine(installPath, "SQRLDotNetClientUI.exe");
        }

        public string GetInstallerExePath(string installPath)
        {
            return Path.Combine(installPath, "SQRLPlatformAwareInstaller_win.exe");
        }

        public DownloadInfo GetDownloadInfoForAsset(GithubRelease selectedRelease)
        {
            return new DownloadInfo
            {
                DownloadSize = Math.Round((selectedRelease.assets.Where(x => x.name.Contains("win-x64.zip")).First().size / 1024M) / 1024M, 2),
                DownloadUrl = selectedRelease.assets.Where(x => x.name.Contains("win-x64.zip")).First().browser_download_url
            };
        }
    }
}
