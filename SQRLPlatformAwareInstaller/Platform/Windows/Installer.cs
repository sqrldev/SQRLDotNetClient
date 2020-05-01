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
        public async Task Install(string archiveFilePath, string installPath)
        {
            Log.Information($"Installing on Windows to {installPath}");

            await Task.Run(() =>
            {
                Log.Information($"Extracting main installation archive");
                Utils.ExtractZipFile(archiveFilePath, string.Empty, installPath);

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
            });

            // Create registry keys for sqrl:// scheme registration
            Log.Information("Creating registry keys for sqrl:// protocol scheme");
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl"))
            {
                key.SetValue(string.Empty, "URL:SQRL Protocol");
                key.SetValue("URL Protocol", $"", RegistryValueKind.String);
            }
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl\DefaultIcon"))
            {
                key.SetValue("", $"{(GetExecutablePath(installPath))},1", RegistryValueKind.String);
            }
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl\shell\open\command"))
            {
                key.SetValue("", $"\"{(GetExecutablePath(installPath))}\" \"%1\"", RegistryValueKind.String);
            }

            //Create Desktop Shortcut
            await Task.Run(() =>
            {
                Log.Information("Create Windows desktop shortcut");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"$SourceFileLocation = \"{GetExecutablePath(installPath)}\"; ");
                sb.AppendLine($"$ShortcutLocation = \"{(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SQRL OSS Client.lnk"))}\"; ");
                sb.AppendLine("$WScriptShell = New-Object -ComObject WScript.Shell; ");
                sb.AppendLine($"$Shortcut = $WScriptShell.CreateShortcut($ShortcutLocation); ");
                sb.AppendLine($"$Shortcut.TargetPath = $SourceFileLocation; ");
                sb.AppendLine($"$Shortcut.IconLocation  = \"{GetExecutablePath(installPath)}\"; ");
                sb.AppendLine($"$Shortcut.WorkingDirectory  = \"{Path.GetDirectoryName(GetExecutablePath(installPath))}\"; ");
                sb.AppendLine($"$Shortcut.Save(); ");
                var tempFile = Path.GetTempFileName().Replace(".tmp", ".ps1");
                File.WriteAllText(tempFile, sb.ToString());

                Process process = new Process();
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.FileName = "powershell";
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.Arguments = $"-File {tempFile}";
                process.Start();
            });
        }

        public Task Uninstall(string uninstallInfoFile)
        {
            throw new NotImplementedException();
        }

        public string GetExecutablePath(string installPath)
        {
            return Path.Combine(installPath, "SQRLDotNetClientUI.exe");
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
