using ReactiveUI;
using SQRLPlatformAwareInstaller.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Linq;
using System.IO;
using Avalonia.Controls;
using SQRLPlatformAwareInstaller.Views;
using Avalonia;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Diagnostics;
using ToolBox.Bridge;
using System.Reflection;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SQRLPlatformAwareInstaller.ViewModels 
{
    public class VersionSelectorViewModel : ViewModelBase
    {

        public static IBridgeSystem _bridgeSystem { get; set; }
        public static ShellConfigurator _shell { get; set; }
        private string platform;
        private WebClient wc;
        private string Executable = "";

        private int _DownloadPercentage;
        public int DownloadPercentage { get { return _DownloadPercentage; } set { this.RaiseAndSetIfChanged(ref _DownloadPercentage, value); } }

        private string DownloadUrl = "";

        private string _InstallationPath;

        private string _warning="";
        
        public string Warning { get { return this._warning; } set { this.RaiseAndSetIfChanged(ref this._warning, value); } }
        public string InstallationPath { get { return this._InstallationPath; } set { this.RaiseAndSetIfChanged(ref this._InstallationPath, value); } }
        public VersionSelectorViewModel(string platform)
        {
            InitObj(platform);
        }
        public VersionSelectorViewModel()
        {
            InitObj();
        }

        private void InitObj(string platform ="WINDOWS")
        {
            this.Title = "SQRL Client Installer - Version Selector";
            this.platform = platform;
            this.wc = new WebClient
            {
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
            };
            
            this.wc.Headers.Add("User-Agent", "SQRL-Intaller");
            
            this.Releases = (Newtonsoft.Json.JsonConvert.DeserializeObject<List<GithubRelease>>(wc.DownloadString("https://api.github.com/repos/sqrldev/SQRLDotNetClient/releases"))).ToArray();
            this.SelectedRelease = this.Releases.OrderByDescending(r => r.published_at).FirstOrDefault();
            
            PathByPlatForm(this.platform);
            if(this.platform=="WINDOWS")
            {
                if(Registry.ClassesRoot.OpenSubKey(@"sqrl") != null)
                {
                    Warning = "WARNING: Exising SQRL Schema Registration Found, if you proceed you will OVERWRITE your existing SQRL installation";
                }
            }
        }

        private void PathByPlatForm(string platform)
        {
            switch (this.platform)
            {

                case "MacOSX":
                {
                    this.InstallationPath = Path.Combine("/Applications/");
                }
                break;
                case "Linux":
                    {
                        this.InstallationPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.None), "SQRL");
                    }
                    break;
                case "WINDOWS":
                default:
                    {
                        this.InstallationPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SQRL");
                    }
                    break;
            }
        }

        private string _installStatus="Installing...";
        public string InstallStatus { get { return this._installStatus; }
        set
            {
                { this.RaiseAndSetIfChanged(ref _installStatus, value); }
            }
        }

        private string DownloadedFileName;

        private GithubRelease[] _release;
        public GithubRelease[] Releases
        {
            get { return this._release; }
            set { this.RaiseAndSetIfChanged(ref _release, value); }
        }

        private GithubRelease _selectedRelease;
        public GithubRelease SelectedRelease
        {
            get { return this._selectedRelease; }
            set { this.RaiseAndSetIfChanged(ref _selectedRelease, value); SetDownloadSize(); }
        }
        decimal? downloadSize;
        public decimal? DownloadSize
        {
            get
            {
                return this.downloadSize;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref downloadSize, value);
            }
            
        }

        public void SetDownloadSize()
        {
            switch (this.platform)
            {

                case "MacOSX":
                    {
                        this.DownloadSize=Math.Round((this.SelectedRelease.assets.Where(x => x.name.Contains("osx-x64.zip")).First().size / 1024M ) / 1024M,2);
                        this.DownloadUrl = this.SelectedRelease.assets.Where(x => x.name.Contains("osx-x64.zip")).First().browser_download_url;
                    }
                    break;
                case "Linux":
                    {
                        this.DownloadSize = Math.Round((this.SelectedRelease.assets.Where(x => x.name.Contains("linux-x64.zip")).First().size / 1024M) / 1024M,2);
                        this.DownloadUrl = this.SelectedRelease.assets.Where(x => x.name.Contains("linux-x64.zip")).First().browser_download_url;
                        
                    }
                    break;
                case "WINDOWS":
                default:
                    {
                        this.DownloadSize=Math.Round((this.SelectedRelease.assets.Where(x => x.name.Contains("win-x64.zip")).First().size / 1024M) / 1024M, 2);
                        this.DownloadUrl = this.SelectedRelease.assets.Where(x => x.name.Contains("win-x64.zip")).First().browser_download_url;
                    }
                    break;
            }
        }

        public void DownloadInstall()
        {
            this.InstallStatus = "Downloading...";
            wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
            wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
            this.DownloadedFileName = Path.GetTempFileName();
            wc.Headers.Remove("Authorization");
            wc.DownloadFileAsync(new Uri(this.DownloadUrl), DownloadedFileName);
        }

        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            this.DownloadPercentage = 100;
            InstallOnPlatform(this.DownloadedFileName);
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new InstallationCompleteViewModel(Path.Combine(this.Executable));
        }

        private void InstallOnPlatform(string downloadedFileName)
        {
            switch (this.platform)
            {
                case "MacOSX":
                    {
                        InstallinMac(downloadedFileName);
                    }
                    break;
                case "Linux":
                    {
                        InstallinLinux(downloadedFileName);
                    }
                    break;
                case "WINDOWS":
                default:
                    {
                        InstallingOnWindows(downloadedFileName);
                    }
                    break;
            }
        }

        private void InstallinMac(string downloadedFileName)
        {
            string fileName = Path.GetTempFileName().Replace(".tmp", ".zip");
            wc.DownloadFile("https://github.com/sqrldev/SQRLDotNetClient/raw/PlatformInstaller/Installers/MacOsX/SQRL.app.zip", fileName);
            //System.IO.Compression.ZipFile.ExtractToDirectory(fileName, this.InstallationPath,true);
            ExtractZipFile(fileName,string.Empty,this.InstallationPath);
            Executable = Path.Combine(this.InstallationPath, "SQRL.app/Contents/MacOS", "SQRLDotNetClientUI");
            this.DownloadPercentage = 20;
            ExtractZipFile(downloadedFileName, string.Empty, Path.Combine(this.InstallationPath, "SQRL.app/Contents/MacOS"));
            //File.Move(downloadedFileName, Executable, true);
            
            File.Copy(Process.GetCurrentProcess().MainModule.FileName, Path.Combine(this.InstallationPath, "SQRL.app/Contents/MacOS", Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)), true);
            this.DownloadPercentage += 20;
                 _bridgeSystem = BridgeSystem.Bash;
            _shell = new ShellConfigurator(_bridgeSystem);
            
            _shell.Term($"chmod a+x {Executable}",Output.Internal);
        }

        private async void InstallinLinux(string downloadedFileName)
        {
            
            this.InstallStatus ="Installing...";
            Executable = Path.Combine(this.InstallationPath, "SQRLDotNetClientUI");
            await Task.Run(() =>
            {
                if (!Directory.Exists(this.InstallationPath))
                {
                    Directory.CreateDirectory(this.InstallationPath);
                }
                this.DownloadPercentage = 20;
                //File.Move(downloadedFileName, Executable, true);
                ExtractZipFile(downloadedFileName, string.Empty, this.InstallationPath);
                
                File.Copy(Process.GetCurrentProcess().MainModule.FileName, Path.Combine(this.InstallationPath, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)), true);
                this.DownloadPercentage += 20;
            });

            _bridgeSystem = BridgeSystem.Bash;
            _shell = new ShellConfigurator(_bridgeSystem);
            
            wc.DownloadFile(@"https://github.com/sqrldev/SQRLDotNetClient/raw/master/SQRLDotNetClientUI/Assets/SQRL_icon_normal_64.png",Path.Combine(this.InstallationPath,"SQRL.png"));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[Desktop Entry]");
            sb.AppendLine("Name=SQRL");
            sb.AppendLine("Type=Application");
            sb.AppendLine($"Icon={(Path.Combine(this.InstallationPath,"SQRL.png"))}");
            sb.AppendLine($"Exec={Executable} %u");
            sb.AppendLine("Categories=Internet");
            sb.AppendLine("Terminal=false");
            sb.AppendLine("MimeType=x-scheme-handler/sqrl");
            File.WriteAllText(Path.Combine(this.InstallationPath,"sqrldev-sqrl.desktop"), sb.ToString());
            _shell.Term($"chmod -R 777 {this.InstallationPath}", Output.Internal);
            _shell.Term($"chmod a+x {Executable}",Output.Internal);
            _shell.Term($"chmod +x {Path.Combine(this.InstallationPath,"sqrldev-sqrl.desktop")}",Output.Internal);
            _shell.Term($"xdg-desktop-menu install {Path.Combine(this.InstallationPath,"sqrldev-sqrl.desktop")}",Output.Internal);
            _shell.Term($"gio mime x-scheme-handler/sqrl sqrldev-sqrl.desktop",Output.Internal);
            _shell.Term($"xdg-mime default sqrldev-sqrl.desktop x-scheme-handler/sqrl",Output.Internal);
            _shell.Term($"update-desktop-database ~/.local/share/applications/",Output.Internal);

        }

        private async void InstallingOnWindows(string downloadedFileName)
        {
            this.InstallStatus = "Installing...";
            Executable = Path.Combine(this.InstallationPath, "SQRLDotNetClientUI.exe");
            Task.Run(() =>
            {
                
                ExtractZipFile(downloadedFileName, string.Empty, this.InstallationPath);
                //var x = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                File.Copy(Process.GetCurrentProcess().MainModule.FileName, Path.Combine(this.InstallationPath, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)), true);
                this.DownloadPercentage += 20;
            }).Wait();
           
            bool cont = true;
        
            if (cont)
            {
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl"))
                {
                    key.SetValue(string.Empty, "URL:SQRL Protocol");
                    key.SetValue("URL Protocol", $"", RegistryValueKind.String);
                    this.DownloadPercentage += 20;
                }
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl\DefaultIcon"))
                {
                    key.SetValue("", $"{(Executable)},1", RegistryValueKind.String);
                    this.DownloadPercentage += 20;
                }
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl\shell\open\command"))
                {
                    key.SetValue("", $"\"{(Executable)}\" \"%1\"", RegistryValueKind.String);
                    this.DownloadPercentage += 20;
                }
            }

  

            //Create Desktop Shortcut
            await Task.Run(() =>
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"$SourceFileLocation = \"{this.Executable}\"; ");
                sb.AppendLine($"$ShortcutLocation = \"{(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"SQRL Dot Net Client.lnk"))}\"; ");
                sb.AppendLine("$WScriptShell = New-Object -ComObject WScript.Shell; ");
                sb.AppendLine($"$Shortcut = $WScriptShell.CreateShortcut($ShortcutLocation); ");
                sb.AppendLine($"$Shortcut.TargetPath = $SourceFileLocation; ");
                sb.AppendLine($"$Shortcut.IconLocation  = \"{this.Executable}\"; ");
                sb.AppendLine($"$Shortcut.WorkingDirectory  = \"{Path.GetDirectoryName(this.Executable)}\"; ");
                sb.AppendLine($"$Shortcut.Save(); ");
                var tempFile = Path.GetTempFileName().Replace(".tmp",".ps1");
                File.WriteAllText(tempFile, sb.ToString());


                Process process = new Process();
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.FileName = "powershell";
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.Arguments = $"-File {tempFile}";
                process.Start();

            });
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.DownloadPercentage = e.ProgressPercentage;
            this.InstallStatus = $"Downloading...{Math.Round(e.BytesReceived/1024M/1024M,2)}/{Math.Round(e.TotalBytesToReceive/1024M / 1024M,2)} MBs";
        }

        public async void FolderPicker()
        {
            OpenFolderDialog ofd = new OpenFolderDialog
            {
                Directory = Path.GetDirectoryName(this.InstallationPath),
                Title = "Select the Destination Directory for the SQRL Client Installation"
            };
            var result = await ofd.ShowAsync(AvaloniaLocator.Current.GetService<MainWindow>());
            if(!string.IsNullOrEmpty(result))
            {
                  switch (this.platform)
                    {

                        case "MacOSX":
                            this.InstallationPath = Path.Combine(result);
                        break;
                        default:
                            this.InstallationPath = Path.Combine(result, "SQRL");
                        break;
                    }
            }
        }

        public void Cancel()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).PriorContent;
        }

        public void ExtractZipFile(string archivePath, string password, string outFolder)
        {

            using (Stream fsInput = File.OpenRead(archivePath))
            using (var zf = new ICSharpCode.SharpZipLib.Zip.ZipFile(fsInput))
            {
                if (!String.IsNullOrEmpty(password))
                {
                    // AES encrypted entries are handled automatically
                    zf.Password = password;
                }
                long fileCt = zf.Count;
                int ct = 0;
                foreach (ZipEntry zipEntry in zf)
                {
                    ct++;
                    this.DownloadPercentage = (int)((ct / fileCt) * 20M);
                    if (!zipEntry.IsFile)
                    {
                        // Ignore directories
                        continue;
                    }
                    String entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:
                    //entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here
                    // to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    // Manipulate the output filename here as desired.
                    var fullZipToPath = Path.Combine(outFolder, entryFileName);
                    if(entryFileName.Equals("sqrl.db", StringComparison.OrdinalIgnoreCase) && File.Exists(entryFileName) )
                    {
                        continue;
                    }
                    var directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                    {
                        if (!Directory.Exists(directoryName))
                        {
                            Directory.CreateDirectory(directoryName);
                            if (this.platform == "WINDOWS")
                            {
                                SetFileAccess(directoryName);
                            }
                        }
                    }

                    // 4K is optimum
                    var buffer = new byte[4096];

                    // Unzip file in buffered chunks. This is just as fast as unpacking
                    // to a buffer the full size of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (var zipStream = zf.GetInputStream(zipEntry))
                    using (Stream fsOutput = File.Create(fullZipToPath))
                    {
                        
                        StreamUtils.Copy(zipStream, fsOutput, buffer);
                        if (this.platform == "WINDOWS")
                        {
                            SetFileAccess(fullZipToPath);
                        }
                    }
                }
            }
        }

        public void SetFileAccess(string file)
        {
            var fi = new System.IO.FileInfo(file);
            var ac = fi.GetAccessControl();
            var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var account = (NTAccount)sid.Translate(typeof(NTAccount));
            var fileAccessRule = new FileSystemAccessRule(account, FileSystemRights.FullControl, AccessControlType.Allow);
            ac.AddAccessRule(fileAccessRule);
            fi.SetAccessControl(ac);
        }

    }
}
