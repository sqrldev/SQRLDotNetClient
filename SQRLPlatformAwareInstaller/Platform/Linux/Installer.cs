using GitHubApi;
using Serilog;
using SQRLCommonUI.Models;
using SQRLPlatformAwareInstaller.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolBox.Bridge;

namespace SQRLPlatformAwareInstaller.Platform.Linux
{
    class Installer : IInstaller
    {
        private static IBridgeSystem _bridgeSystem { get; set; } = BridgeSystem.Bash;
        private static ShellConfigurator _shell { get; set; } = new ShellConfigurator(_bridgeSystem);

        public async Task Install(string archiveFilePath, string installPath, string versionTag)
        {
            Log.Information("Installing on Linux");
            Inventory.Instance.Load();

            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            await Task.Run(() =>
            {
                Log.Information($"Extracting main installation archive");
                Utils.ExtractZipFile(archiveFilePath, string.Empty, installPath);
                Inventory.Instance.AddDirectory(installPath);

                Log.Information("Copying installer into installation location (for auto update)");
                try
                {
                    //Copy the installer but don't over-write the one included in the zip since it will likely be newer
                    File.Copy(Process.GetCurrentProcess().MainModule.FileName, 
                        Path.Combine(installPath, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)), false);
                }
                catch (Exception fc)
                {
                    Log.Warning($"File copy exception while copying installer:\r\n{fc}");
                }
            });

            Log.Information("Creating Linux desktop icon, application and registering SQRL invokation scheme");
            await Task.Run(() =>
            {
                GitHubHelper.DownloadFile(@"https://github.com/sqrldev/SQRLDotNetClient/raw/master/SQRLDotNetClientUI/Assets/SQRL_icon_normal_64.png",
                    Path.Combine(installPath, "SQRL.png"));
            });

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[Desktop Entry]");
            sb.AppendLine("Name=SQRL");
            sb.AppendLine("Type=Application");
            sb.AppendLine($"Icon={(Path.Combine(installPath, "SQRL.png"))}");
            sb.AppendLine($"Exec={GetClientExePath(installPath)} %u");
            sb.AppendLine("Categories=Internet");
            sb.AppendLine("Terminal=false");
            sb.AppendLine("MimeType=x-scheme-handler/sqrl");
            File.WriteAllText(Path.Combine(installPath, "sqrldev-sqrl.desktop"), sb.ToString());

            _shell.Term($"chmod -R 755 {installPath}", Output.Internal);
            _shell.Term($"chmod a+x {GetClientExePath(installPath)}", Output.Internal);
            _shell.Term($"chmod +x {Path.Combine(installPath, "sqrldev-sqrl.desktop")}", Output.Internal);
            _shell.Term($"chmod a+x {Path.Combine(installPath, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName))}", Output.Internal);
            _shell.Term($"xdg-desktop-menu install {Path.Combine(installPath, "sqrldev-sqrl.desktop")}", Output.Internal);
            _shell.Term($"gio mime x-scheme-handler/sqrl sqrldev-sqrl.desktop", Output.Internal);
            _shell.Term($"xdg-mime default sqrldev-sqrl.desktop x-scheme-handler/sqrl", Output.Internal);
            _shell.Term($"update-desktop-database ~/.local/share/applications/", Output.Internal);

            Inventory.Instance.Save();
        }

        public async Task Uninstall(IProgress<Tuple<int, string>> progress = null, bool dryRun = true)
        {
            // First, remove the desktop entry and "sqrl://" scheme handlers
            progress.Report(new Tuple<int, string>(0, $"Removing desktop entries and sqrl:// scheme handlers"));
            var desktopFile = Path.Combine(PathConf.ClientInstallPath, "sqrldev-sqrl.desktop");
            _shell.Term($"xdg-mime uninstall {desktopFile}", Output.Internal);
            _shell.Term($"xdg-desktop-menu uninstall sqrldev-sqrl.desktop", Output.Internal);
            _shell.Term($"update-desktop-database ~/.local/share/applications/", Output.Internal);

            // Run the inventory-based uninstaller
            await Uninstaller.Run(progress, dryRun);
        }

        public string GetClientExePath(string installPath)
        {
            return Path.Combine(installPath, "SQRLDotNetClientUI");
        }

        public string GetInstallerExePath(string installPath)
        {
            return Path.Combine(installPath, "SQRLPlatformAwareInstaller_linux");
        }

        public DownloadInfo GetDownloadInfoForAsset(GithubRelease selectedRelease)
        {
            return new DownloadInfo
            {
                DownloadSize = Math.Round((selectedRelease.assets.Where(x => x.name.Contains("linux-x64.zip")).First().size / 1024M) / 1024M, 2),
                DownloadUrl = selectedRelease.assets.Where(x => x.name.Contains("linux-x64.zip")).First().browser_download_url
            };
        }
    }
}
