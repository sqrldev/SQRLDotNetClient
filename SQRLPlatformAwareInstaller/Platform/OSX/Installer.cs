using GitHubApi;
using Serilog;
using SQRLPlatformAwareInstaller.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToolBox.Bridge;

namespace SQRLPlatformAwareInstaller.Platform.OSX
{
    class Installer : IInstaller
    {
        private static IBridgeSystem _bridgeSystem { get; set; } = BridgeSystem.Bash;
        private static ShellConfigurator _shell { get; set; } = new ShellConfigurator(_bridgeSystem);

        public async Task Install(string archiveFilePath, string installPath, string versionTag)
        {
            Log.Information($"Installing on macOS to {installPath}");
            string fileName = Path.GetTempFileName().Replace(".tmp", ".zip");

            await Task.Run(() =>
            {
                Log.Information("Downloading Mac app folder structure from Github");
                GitHubHelper.DownloadFile("https://github.com/sqrldev/SQRLDotNetClient/raw/PlatformInstaller/Installers/MacOsX/SQRL.app.zip", fileName);

                Log.Information("Creating initial SQRL application template");
                Utils.ExtractZipFile(fileName, string.Empty, installPath);
            });
            
            await Task.Run(() =>
            {
                Log.Information($"Extracting main installation archive");
                Utils.ExtractZipFile(archiveFilePath, string.Empty, Path.Combine(installPath, "SQRL.app/Contents/MacOS"));
                try
                {
                    Log.Information("Copying installer into installation location (for auto update)");
                    File.Copy(Process.GetCurrentProcess().MainModule.FileName, Path.Combine(
                        installPath, "SQRL.app/Contents/MacOS",
                        Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)), false);
                }
                catch (Exception fc)
                {
                    Log.Error($"File copy exception: {fc}");
                }
            });

            Log.Information("Changing executable file to be executable a+x");
            _shell.Term($"chmod a+x {GetClientExePath(installPath)}", Output.Internal);
            _shell.Term($"chmod a+x {Path.Combine(installPath, "SQRL.app/Contents/MacOS", Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName))}", Output.Internal);
        }

        public async Task Uninstall(IProgress<Tuple<int, string>> progress = null, bool dryRun = true)
        {
            await Uninstaller.Run(progress, dryRun);
        }

        public string GetClientExePath(string installPath)
        {
            return Path.Combine(installPath, "SQRL.app/Contents/MacOS", "SQRLDotNetClientUI");
        }

        public string GetInstallerExePath(string installPath)
        {
            return Path.Combine(installPath, "SQRLPlatformAwareInstaller_osx");
        }

        public DownloadInfo GetDownloadInfoForAsset(GithubRelease selectedRelease)
        {
            return new DownloadInfo
            {
                DownloadSize = Math.Round((selectedRelease.assets.Where(x => x.name.Contains("osx-x64.zip")).First().size / 1024M) / 1024M, 2),
                DownloadUrl = selectedRelease.assets.Where(x => x.name.Contains("osx-x64.zip")).First().browser_download_url
            };
        }
    }
}
