using Avalonia;
using Avalonia.Platform;
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

#pragma warning disable 1998
        public async Task Install(string archiveFilePath, string installPath, string versionTag)
        {
            // The "async" in the delegate is needed, otherwise exceptions within
            // the delegate won't "bubble up" to the exception handlers upstream.
            await Task.Run(() =>
            {
                Inventory.Instance.Load();
                Log.Information($"Installing on Linux to {installPath}");

                // Extract main installation archive
                Log.Information($"Extracting main installation archive");
                Utils.ExtractZipFile(archiveFilePath, string.Empty, installPath);

                // Check if a database exists in the installation directory 
                // (which is bad) and if it does, move it to user space.
                if (File.Exists(Path.Combine(installPath, PathConf.DBNAME)))
                {
                    Utils.MoveDb(Path.Combine(installPath, PathConf.DBNAME));
                }

                Inventory.Instance.AddDirectory(installPath);

                // Create icon, register sqrl:// scheme etc.
                Log.Information("Creating Linux desktop icon, application and registering SQRL invokation scheme");
                GitHubHelper.DownloadFile(@"https://github.com/sqrldev/SQRLDotNetClient/raw/master/SQRLDotNetClientUI/Assets/SQRL_icon_normal_64.png",
                        Path.Combine(installPath, "SQRL.png"));

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
                _shell.Term($"chmod +x {GetClientExePath(installPath)}", Output.Internal);
                _shell.Term($"chmod +x {Path.Combine(installPath, "sqrldev-sqrl.desktop")}", Output.Internal);
                _shell.Term($"chmod +x {Path.Combine(installPath, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName))}", Output.Internal);
                _shell.Term($"xdg-desktop-menu install {Path.Combine(installPath, "sqrldev-sqrl.desktop")}", Output.Internal);
                _shell.Term($"gio mime x-scheme-handler/sqrl sqrldev-sqrl.desktop", Output.Internal);
                _shell.Term($"xdg-mime default sqrldev-sqrl.desktop x-scheme-handler/sqrl", Output.Internal);
                string user = _shell.Term("logname", Output.Hidden).stdout.Trim();
                string home = _shell.Term($"getent passwd {user} | cut -d: -f6", Output.Hidden).stdout.Trim();
                if (AdminCheck.IsAdmin())
                {
                    _shell.Term($"update-desktop-database {home}/.local/share/applications/", Output.Internal);
                }
                else
                {
                    _shell.Term($"update-desktop-database ~/.local/share/applications/", Output.Internal);
                }
                

                // Change owner of database dir/file to the actual user behind the "sudo"
                
                string chownDbFile = $"chown -R {user}:{user} {PathConf.ClientDBPath}";
                Log.Information($"Determined username for chown: \"{user}\"");
                Log.Information($"Running command: {chownDbFile}");
                _shell.Term(chownDbFile, Output.Internal);

                Log.Information("All us good up to this point, lets setup Linux for UAC (if we can)");

                /*
                 * Creates the required file and system changes for SQRL to be available
                 * ubiquitous throughout the system via a new 
                 * environment variable SQRL_HOME and the addition of this variable to the system PATH.
                 * 
                 * Note that the later won't take effect until the user logs out or reboots
                 */
                var result = _shell.Term("command -v pkexec", Output.Internal);
                if (string.IsNullOrEmpty(result.stderr) && !string.IsNullOrEmpty(result.stdout))
                {
                    Log.Information("Creating SQRL_HOME Environment Variable and adding SQRL_HOME to PATH");
                    string sqrlvarsFile = "/etc/profile.d/sqrl-vars.sh";
                    using (StreamWriter sw = new StreamWriter(sqrlvarsFile))
                    {
                        sw.WriteLine($"export SQRL_HOME={installPath}");
                        sw.WriteLine("export PATH=$PATH:$SQRL_HOME");
                        sw.Close();
                    }
                    Inventory.Instance.AddFile(sqrlvarsFile);
                    Log.Information("Creating polkit rule for SQRL");
                    var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                    string sqrlPolkitPolicyFile = Path.Combine("/usr/share/polkit-1/actions", "org.freedesktop.policykit.SQRLPlatformAwareInstaller_linux.policy");
                    using (StreamWriter sw = new StreamWriter(sqrlPolkitPolicyFile))
                    {
                        string policyFile = "";
                        using (var stream = new StreamReader(assets.Open(new Uri("resm:SQRLPlatformAwareInstaller.Assets.SQRLPlatformAwareInstaller_linux.policy"))))
                        {
                            policyFile = stream.ReadToEnd();
                        }
                        policyFile = policyFile.Replace("INSTALLER_PATH", Path.Combine(installPath, "SQRLPlatformAwareInstaller_linux"));
                        sw.Write(policyFile);
                        sw.Close();
                    }
                    _shell.Term("export SQRL_HOME={installPath}", Output.Internal);
                    _shell.Term("export PATH=$PATH:$SQRL_HOME", Output.Internal);
                    Inventory.Instance.AddFile(sqrlPolkitPolicyFile);

                }
                else
                {
                    Log.Warning("pkexec was not found , we can't automatically elevate permissions UAC style, user will have to do manually");
                }

                Inventory.Instance.Save();
            });
        }
#pragma warning restore 1998

        public async Task Uninstall(IProgress<Tuple<int, string>> progress = null, bool dryRun = true)
        {
            // First, remove the desktop entry and "sqrl://" scheme handlers
            progress.Report(new Tuple<int, string>(0, $"Removing desktop entries and sqrl:// scheme handlers"));
            var desktopFile = Path.Combine(PathConf.ClientInstallPath, "sqrldev-sqrl.desktop");
            _shell.Term($"xdg-mime uninstall {desktopFile}", Output.Internal);
            _shell.Term($"xdg-desktop-menu uninstall sqrldev-sqrl.desktop", Output.Internal);
            string user = _shell.Term("logname", Output.Hidden).stdout.Trim();
            string home = _shell.Term($"getent passwd {user} | cut -d: -f6", Output.Hidden).stdout.Trim();
            if (AdminCheck.IsAdmin())
            {
                _shell.Term($"update-desktop-database {home}/.local/share/applications/", Output.Internal);
            }
            else
            {
                _shell.Term($"update-desktop-database ~/.local/share/applications/", Output.Internal);
            }
            

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
