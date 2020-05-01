using GitHubApi;
using Serilog;
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

        public async Task Install(string archiveFilePath, string installPath)
        {
            Log.Information("Installing on Linux");

            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            await Task.Run(() =>
            {
                Log.Information($"Extracting main installation archive");
                Utils.ExtractZipFile(archiveFilePath, string.Empty, installPath);

                Log.Information("Copying installer into installation location (for auto update)");
                try
                {
                    //Copy the installer but don't over-write the one included in the zip since it will likely be newer
                    File.Copy(Process.GetCurrentProcess().MainModule.FileName, 
                        Path.Combine(installPath, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)), false);
                }
                catch (Exception fc)
                {
                    Log.Warning($"File copy exception: {fc}");
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
            sb.AppendLine($"Exec={GetExecutablePath(installPath)} %u");
            sb.AppendLine("Categories=Internet");
            sb.AppendLine("Terminal=false");
            sb.AppendLine("MimeType=x-scheme-handler/sqrl");
            File.WriteAllText(Path.Combine(installPath, "sqrldev-sqrl.desktop"), sb.ToString());

            _shell.Term($"chmod -R 755 {installPath}", Output.Internal);
            _shell.Term($"chmod a+x {GetExecutablePath(installPath)}", Output.Internal);
            _shell.Term($"chmod +x {Path.Combine(installPath, "sqrldev-sqrl.desktop")}", Output.Internal);
            _shell.Term($"chmod a+x {Path.Combine(installPath, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName))}", Output.Internal);
            _shell.Term($"xdg-desktop-menu install {Path.Combine(installPath, "sqrldev-sqrl.desktop")}", Output.Internal);
            _shell.Term($"gio mime x-scheme-handler/sqrl sqrldev-sqrl.desktop", Output.Internal);
            _shell.Term($"xdg-mime default sqrldev-sqrl.desktop x-scheme-handler/sqrl", Output.Internal);
            _shell.Term($"update-desktop-database ~/.local/share/applications/", Output.Internal);
        }

        public Task Uninstall(string uninstallInfoFile)
        {
            throw new NotImplementedException();
        }

        public string GetExecutablePath(string installPath)
        {
            return Path.Combine(installPath, "SQRLDotNetClientUI");
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
