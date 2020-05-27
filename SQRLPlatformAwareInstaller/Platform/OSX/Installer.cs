using Serilog;
using SQRLCommon.Models;
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

        #pragma warning disable 1998
        public async Task Install(string archiveFilePath, string installPath, string versionTag)
        {
            // The "async" in the delegate is needed, otherwise exceptions within
            // the delegate won't "bubble up" to the exception handlers upstream.
            await Task.Run(async () =>
            {
                Inventory.Instance.Load();
                Log.Information($"Installing on macOS to {installPath}");

                string fileName = Path.GetTempFileName().Replace(".tmp", ".zip");

                // Download an extract initial SQRL application template
                Log.Information("Downloading Mac app folder structure from Github");
                GithubHelper.DownloadFile("https://github.com/sqrldev/SQRLDotNetClient/raw/PlatformInstaller/Installers/MacOsX/SQRL.app.zip", fileName);
                Log.Information("Creating initial SQRL application template");
                Utils.ExtractZipFile(fileName, string.Empty, installPath);
                File.Delete(fileName);

                // Extract main installation archive
                Log.Information($"Extracting main installation archive");
                Utils.ExtractZipFile(archiveFilePath, string.Empty, Path.Combine(installPath, "SQRL.app/Contents/MacOS"));

                // Check if a database exists in the installation directory 
                // (which is bad) and if it does, move it to user space.
                if (File.Exists(Path.Combine(installPath, "SQRL.app/Contents/MacOS", PathConf.DBNAME)))
                {
                    Utils.MoveDb(Path.Combine(installPath, "SQRL.app/Contents/MacOS", PathConf.DBNAME));
                }

                Inventory.Instance.AddDirectory(Path.Combine(installPath, "SQRL.app"));

                // Set executable bit on executables
                Log.Information("Changing executable file to be executable a+x");
                _shell.Term($"chmod a+x {GetClientExePath(installPath)}", Output.Internal);
                _shell.Term($"chmod a+x {Path.Combine(installPath, "SQRL.app/Contents/MacOS", Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName))}", Output.Internal);

                Inventory.Instance.Save();
            });
        }
        #pragma warning restore 1998

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
