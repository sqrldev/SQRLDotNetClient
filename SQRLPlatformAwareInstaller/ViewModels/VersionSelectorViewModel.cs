using ReactiveUI;
using System;
using System.Net;
using System.Text;
using System.Linq;
using System.IO;
using Avalonia.Controls;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Diagnostics;
using ToolBox.Bridge;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.Security.AccessControl;
using System.Security.Principal;
using GitHubApi;
using Serilog;
using System.Runtime.InteropServices;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    /// <summary>
    /// A view model representing the version selection screen
    /// of the SQRL installer.
    /// </summary>
    public class VersionSelectorViewModel : ViewModelBase
    {
        private static IBridgeSystem _bridgeSystem { get; set; }
        private static ShellConfigurator _shell { get; set; }
        private WebClient _webClient;
        private string _executable = "";
        private int _downloadPercentage;
        private string _downloadUrl = "";
        private string _installationPath;
        private string _warning = "";
        private string _installStatus = "";
        private string _downloadedFileName;
        private GithubRelease[] _releases;
        private GithubRelease _selectedRelease;
        private decimal? _downloadSize;
        private bool _enablePreReleases = false;
        private bool _hasReleases = false;

        /// <summary>
        /// Gets or sets the download progress percentage.
        /// </summary>
        public int DownloadPercentage 
        { 
            get { return _downloadPercentage; } 
            set { this.RaiseAndSetIfChanged(ref _downloadPercentage, value); } 
        }

        /// <summary>
        /// Gets or sets the installation warning message.
        /// </summary>
        public string Warning 
        { 
            get { return this._warning; } 
            set { this.RaiseAndSetIfChanged(ref this._warning, value); } 
        }

        /// <summary>
        /// Gets or sets the program installation path.
        /// </summary>
        public string InstallationPath 
        { 
            get  { return this._installationPath; } 
            set { this.RaiseAndSetIfChanged(ref this._installationPath, value); } 
        }

        /// <summary>
        /// Gets or sets a string representing the installation status.
        /// </summary>
        public string InstallStatus 
        {
            get { return this._installStatus; }
            set
            {
                { this.RaiseAndSetIfChanged(ref _installStatus, value); }
            }
        }

        /// <summary>
        /// Gets or sets an array of app release information.
        /// </summary>
        public GithubRelease[] Releases
        {
            get { return this._releases; }
            set { this.RaiseAndSetIfChanged(ref _releases, value); }
        }

        /// <summary>
        /// Gets or sets the selected app release.
        /// </summary>
        public GithubRelease SelectedRelease
        {
            get { return this._selectedRelease; }
            set { this.RaiseAndSetIfChanged(ref _selectedRelease, value); SetDownloadSize(); }
        }

        /// <summary>
        /// Gets or sets the download size.
        /// </summary>
        public decimal? DownloadSize
        {
            get { return this._downloadSize; }
            set { this.RaiseAndSetIfChanged(ref _downloadSize, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to enable fetching
        /// alpha/beta releases.
        /// </summary>
        public bool EnablePreReleases
        {
            get { return this._enablePreReleases; }
            set { this.RaiseAndSetIfChanged(ref _enablePreReleases, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether any releases are available.
        /// </summary>
        public bool HasReleases
        {
            get { return this._hasReleases; }
            set { this.RaiseAndSetIfChanged(ref _hasReleases, value); }
        }

        /// <summary>
        /// Creates a new instance and performs some initializations.
        /// </summary>
        public VersionSelectorViewModel()
        {
            Log.Information("Version selection screen launched");
            Init();
        }

        /// <summary>
        /// Performs initialization tasks.
        /// </summary>
        private void Init()
        {
            this.Title = _loc.GetLocalizationValue("TitleVersionSelector");

            this.WhenAnyValue(x => x.EnablePreReleases)
                .Subscribe(x => GetReleases());

            GetReleases();
        }

        /// <summary>
        /// Fetches available releases from Github.
        /// </summary>
        private async void GetReleases()
        {
            _webClient = new WebClient
            {
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
            };

            _webClient.Headers.Add("User-Agent", GitHubHelper.SQRLInstallerUserAgent);

            this.Releases = await GitHubHelper.GetReleases(this.EnablePreReleases);
            this.HasReleases = this.Releases.Length > 0;

            if (this.HasReleases)
            {
                this.SelectedRelease = this.Releases.OrderByDescending(r => r.published_at).FirstOrDefault();
                Log.Information($"Found {this.Releases?.Count()} Releases");
                PathByPlatform();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Log.Information($"We are on Windows, checking to see if an existing version of SQRL exists");
                    if (Registry.ClassesRoot.OpenSubKey(@"sqrl") != null)
                    {
                        Log.Information($"Display Warning that we may over-write the existing SQRL Install");
                        this.Warning = _loc.GetLocalizationValue("SchemaRegistrationWarning");
                    }
                }
            }
        }

        /// <summary>
        /// Sets a default installation path according to the detected platform.
        /// </summary>
        private void PathByPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                this.InstallationPath = Path.Combine("/Applications/");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                this.InstallationPath = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.None), "SQRL");
            }
            else
            {
                this.InstallationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SQRL");
            }

            Log.Information($"Set installation path to: {this.InstallationPath}");
        }

        /// <summary>
        /// Calculates and sets the download size for the selected release.
        /// </summary>
        public void SetDownloadSize()
        {
            if (SelectedRelease == null) return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                this.DownloadSize = Math.Round((this.SelectedRelease.assets.Where(x => x.name.Contains("osx-x64.zip")).First().size / 1024M) / 1024M, 2);
                this._downloadUrl = this.SelectedRelease.assets.Where(x => x.name.Contains("osx-x64.zip")).First().browser_download_url;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                this.DownloadSize = Math.Round((this.SelectedRelease.assets.Where(x => x.name.Contains("linux-x64.zip")).First().size / 1024M) / 1024M, 2);
                this._downloadUrl = this.SelectedRelease.assets.Where(x => x.name.Contains("linux-x64.zip")).First().browser_download_url;
            }
            else
            {
                this.DownloadSize = Math.Round((this.SelectedRelease.assets.Where(x => x.name.Contains("win-x64.zip")).First().size / 1024M) / 1024M, 2);
                this._downloadUrl = this.SelectedRelease.assets.Where(x => x.name.Contains("win-x64.zip")).First().browser_download_url;
            }

            Log.Information($"Current download size is : {this.DownloadSize} MB");
            Log.Information($"Current download URL is : {this._downloadUrl}");
        }

        /// <summary>
        /// Downloads the selected release file to a temporary file.
        /// </summary>
        public void DownloadInstall()
        {
            if (string.IsNullOrEmpty(this._downloadUrl)) return;

            this.InstallStatus = _loc.GetLocalizationValue("InstallStatusDownloading");
            _webClient.DownloadProgressChanged += Wc_DownloadProgressChanged;
            _webClient.DownloadFileCompleted += Wc_DownloadFileCompleted;

            this._downloadedFileName = Path.GetTempFileName();
            Log.Information($"Temporary download file name: {_downloadedFileName}");
            _webClient.DownloadFileAsync(new Uri(this._downloadUrl), _downloadedFileName);
        }

        /// <summary>
        /// Event handler for the "download completed" event.
        /// </summary>
        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Log.Information("Download completed");
            this.DownloadPercentage = 100;
            InstallOnPlatform(this._downloadedFileName);
            ((MainWindowViewModel)_mainWindow.DataContext).Content = new InstallationCompleteViewModel(Path.Combine(this._executable));
        }

        /// <summary>
        /// Installs the app in <paramref name="downloadedFileName"/> according
        /// to the detected platform.
        /// </summary>
        /// <param name="downloadedFileName">The downloaded application files to install.</param>
        private void InstallOnPlatform(string downloadedFileName)
        {
            Log.Information($"Launching installation");

            _installStatus = _loc.GetLocalizationValue("InstallStatusInstalling");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                InstallOnWindows(downloadedFileName);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                InstallOnMac(downloadedFileName);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                InstallOnLinux(downloadedFileName);
        }

        /// <summary>
        /// Installs the app in <paramref name="downloadedFileName"/> on MacOSX.
        /// </summary>
        /// <param name="downloadedFileName">The downloaded application files to install.</param>
        private void InstallOnMac(string downloadedFileName)
        {
            Log.Information("Installing on MacOSX");
            string fileName = Path.GetTempFileName().Replace(".tmp", ".zip");

            Log.Information("Downloading Mac app folder structure from Github");
            GitHubHelper.DownloadFile("https://github.com/sqrldev/SQRLDotNetClient/raw/PlatformInstaller/Installers/MacOsX/SQRL.app.zip", fileName);

            Log.Information("Creating initial SQRL application template");
            ExtractZipFile(fileName, string.Empty, this.InstallationPath);
            _executable = Path.Combine(this.InstallationPath, "SQRL.app/Contents/MacOS", "SQRLDotNetClientUI");
            Log.Information($"Excecutable location:{_executable}");
            this.DownloadPercentage = 20;
            ExtractZipFile(downloadedFileName, string.Empty, Path.Combine(this.InstallationPath, "SQRL.app/Contents/MacOS"));
            //File.Move(downloadedFileName, Executable, true);
            try
            {
                Log.Information("Copying installer into installation location (for auto update)");
                File.Copy(Process.GetCurrentProcess().MainModule.FileName, Path.Combine(this.InstallationPath, "SQRL.app/Contents/MacOS", Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)), false);
            }
            catch (Exception fc)
            {
                Log.Error($"File copy exception: {fc}");
            }
            using (StreamWriter sw = new StreamWriter(Path.Combine(this.InstallationPath, "SQRL.app/Contents/MacOS", "sqrlversion.json")))
            {
                Log.Information($"Finished installing SQRL version: {this.SelectedRelease.tag_name}");
                sw.Write(Newtonsoft.Json.JsonConvert.SerializeObject(this.SelectedRelease.tag_name));
                sw.Close();
            }

            this.DownloadPercentage += 20;
            _bridgeSystem = BridgeSystem.Bash;
            _shell = new ShellConfigurator(_bridgeSystem);
            Log.Information("Changing executable file to be executable a+x");
            _shell.Term($"chmod a+x {_executable}", Output.Internal);
            _shell.Term($"chmod a+x {Path.Combine(this.InstallationPath, "SQRL.app/Contents/MacOS", Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName))}", Output.Internal);
        }

        /// <summary>
        /// Installs the app in <paramref name="downloadedFileName"/> on Linux.
        /// </summary>
        /// <param name="downloadedFileName">The downloaded application files to install.</param>
        private void InstallOnLinux(string downloadedFileName)
        {
            Log.Information("Installing on Linux");

            _executable = Path.Combine(this.InstallationPath, "SQRLDotNetClientUI");

            if (!Directory.Exists(this.InstallationPath))
            {
                Directory.CreateDirectory(this.InstallationPath);
            }

            this.DownloadPercentage = 20;
            //File.Move(downloadedFileName, Executable, true);
            ExtractZipFile(downloadedFileName, string.Empty, this.InstallationPath);
            try
            {
                Log.Information("Copying installer into installation location (for auto update)");
                //Copy the installer but don't over-write the one included in the zip since it will likely be newer
                File.Copy(Process.GetCurrentProcess().MainModule.FileName, Path.Combine(this.InstallationPath, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)), false);
            }
            catch (Exception fc)
            {
                Log.Warning($"File copy exception: {fc}");
            }
            using (StreamWriter sw = new StreamWriter(Path.Combine(this.InstallationPath, "sqrlversion.json")))
            {
                sw.Write(Newtonsoft.Json.JsonConvert.SerializeObject(this.SelectedRelease.tag_name));
                sw.Close();
            }
            this.DownloadPercentage += 20;


            _bridgeSystem = BridgeSystem.Bash;
            _shell = new ShellConfigurator(_bridgeSystem);

            Log.Information("Creating Linux desktop icon, application and registering SQRL invokation scheme");
            GitHubHelper.DownloadFile(@"https://github.com/sqrldev/SQRLDotNetClient/raw/master/SQRLDotNetClientUI/Assets/SQRL_icon_normal_64.png", Path.Combine(this.InstallationPath, "SQRL.png"));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[Desktop Entry]");
            sb.AppendLine("Name=SQRL");
            sb.AppendLine("Type=Application");
            sb.AppendLine($"Icon={(Path.Combine(this.InstallationPath, "SQRL.png"))}");
            sb.AppendLine($"Exec={_executable} %u");
            sb.AppendLine("Categories=Internet");
            sb.AppendLine("Terminal=false");
            sb.AppendLine("MimeType=x-scheme-handler/sqrl");
            File.WriteAllText(Path.Combine(this.InstallationPath, "sqrldev-sqrl.desktop"), sb.ToString());
            _shell.Term($"chmod -R 755 {this.InstallationPath}", Output.Internal);
            _shell.Term($"chmod a+x {_executable}", Output.Internal);
            _shell.Term($"chmod +x {Path.Combine(this.InstallationPath, "sqrldev-sqrl.desktop")}", Output.Internal);
            _shell.Term($"chmod a+x {Path.Combine(this.InstallationPath, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName))}", Output.Internal);
            _shell.Term($"xdg-desktop-menu install {Path.Combine(this.InstallationPath, "sqrldev-sqrl.desktop")}", Output.Internal);
            _shell.Term($"gio mime x-scheme-handler/sqrl sqrldev-sqrl.desktop", Output.Internal);
            _shell.Term($"xdg-mime default sqrldev-sqrl.desktop x-scheme-handler/sqrl", Output.Internal);
            _shell.Term($"update-desktop-database ~/.local/share/applications/", Output.Internal);
        }

        /// <summary>
        /// Installs the app in <paramref name="downloadedFileName"/> on Windows.
        /// </summary>
        /// <param name="downloadedFileName">The downloaded application files to install.</param>
        private async void InstallOnWindows(string downloadedFileName)
        {
            Log.Information("Installing on Windows");

            _executable = Path.Combine(this.InstallationPath, "SQRLDotNetClientUI.exe");
            Task.Run(() =>
            {
                ExtractZipFile(downloadedFileName, string.Empty, this.InstallationPath);

                try
                {
                    Log.Information("Copying installer into installation location (for auto update)");
                    File.Copy(Process.GetCurrentProcess().MainModule.FileName, Path.Combine(this.InstallationPath, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)), false);
                }
                catch (Exception fc)
                {
                    Log.Warning($"File copy exception {fc}");
                }
                using (StreamWriter sw = new StreamWriter(Path.Combine(this.InstallationPath, "sqrlversion.json")))
                {
                    sw.Write(Newtonsoft.Json.JsonConvert.SerializeObject(this.SelectedRelease.tag_name));
                    sw.Close();
                }
                this.DownloadPercentage += 20;
            }).Wait();

            bool cont = true;

            if (cont)
            {
                Log.Information("Creating registry keys for sqrl:// protocol scheme");
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl"))
                {
                    key.SetValue(string.Empty, "URL:SQRL Protocol");
                    key.SetValue("URL Protocol", $"", RegistryValueKind.String);
                    this.DownloadPercentage += 20;
                }
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl\DefaultIcon"))
                {
                    key.SetValue("", $"{(_executable)},1", RegistryValueKind.String);
                    this.DownloadPercentage += 20;
                }
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl\shell\open\command"))
                {
                    key.SetValue("", $"\"{(_executable)}\" \"%1\"", RegistryValueKind.String);
                    this.DownloadPercentage += 20;
                }
            }

            //Create Desktop Shortcut
            await Task.Run(() =>
            {
                Log.Information("Create Windows desktop shortcut");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"$SourceFileLocation = \"{this._executable}\"; ");
                sb.AppendLine($"$ShortcutLocation = \"{(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SQRL OSS Client.lnk"))}\"; ");
                sb.AppendLine("$WScriptShell = New-Object -ComObject WScript.Shell; ");
                sb.AppendLine($"$Shortcut = $WScriptShell.CreateShortcut($ShortcutLocation); ");
                sb.AppendLine($"$Shortcut.TargetPath = $SourceFileLocation; ");
                sb.AppendLine($"$Shortcut.IconLocation  = \"{this._executable}\"; ");
                sb.AppendLine($"$Shortcut.WorkingDirectory  = \"{Path.GetDirectoryName(this._executable)}\"; ");
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

        /// <summary>
        /// Event handler for "download progress changed" event.
        /// </summary>
        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.DownloadPercentage = e.ProgressPercentage;
            this.InstallStatus = _loc.GetLocalizationValue("InstallStatusDownloading") +  
                $" {Math.Round(e.BytesReceived / 1024M / 1024M, 2)}/{Math.Round(e.TotalBytesToReceive / 1024M / 1024M, 2)} MB";
        }

        /// <summary>
        /// Opens a dialog to let the user select an installation folder.
        /// </summary>
        public async void FolderPicker()
        {
            try
            {
                OpenFolderDialog ofd = new OpenFolderDialog
                {
                    Directory = Path.GetDirectoryName(this.InstallationPath),
                    Title = _loc.GetLocalizationValue("TitleChooseInstallFolderDialog")
                };
                var result = await ofd.ShowAsync(_mainWindow);
                if (!string.IsNullOrEmpty(result))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        this.InstallationPath = Path.Combine(result);
                    else
                        this.InstallationPath = Path.Combine(result, "SQRL");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error showing install folder selection dialog!");
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
                throw (ex);
            }
        }

        /// <summary>
        /// Goes back to the previous screen.
        /// </summary>
        public void Back()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
        }

        /// <summary>
        /// Extracts the zip archive specified by <paramref name="archivePath"/> into the
        /// output directory <paramref name="outFolder"/> using <paramref name="password"/>.
        /// </summary>
        /// <param name="archivePath">The archive to extract.</param>
        /// <param name="password">The password for the archive.</param>
        /// <param name="outFolder">The output folder.</param>
        public void ExtractZipFile(string archivePath, string password, string outFolder)
        {
            using (Stream fsInput = File.OpenRead(archivePath))
            {
                using (var zf = new ZipFile(fsInput))
                {
                    //We don't password protect our install but maybe we should
                    if (!String.IsNullOrEmpty(password))
                    {
                        // AES encrypted entries are handled automatically
                        zf.Password = password;
                    }

                    long fileCt = zf.Count;
                    
                    foreach (ZipEntry zipEntry in zf)
                    {
                        
                        if (!zipEntry.IsFile)
                        {
                            // Ignore directories
                            continue;
                        }
                        String entryFileName = zipEntry.Name;
                      

                        // Manipulate the output filename here as desired.
                        var fullZipToPath = Path.Combine(outFolder, entryFileName);
                        //Do not over-write the sqrl Db if it exists
                        
                        if (entryFileName.Equals("sqrl.db", StringComparison.OrdinalIgnoreCase) && File.Exists(fullZipToPath))
                        {
                            Log.Information("Found existing SQRL DB , keeping existing");
                            continue;
                        }
                        var directoryName = Path.GetDirectoryName(fullZipToPath);
                        if (directoryName.Length > 0)
                        {
                            if (!Directory.Exists(directoryName))
                            {
                                Directory.CreateDirectory(directoryName);
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                SetFileAccess(fullZipToPath);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Grants full access permissions for <paramref name="file"/> to the current user.
        /// </summary>
        /// <param name="file">The file to set the permissions for.</param>
        public void SetFileAccess(string file)
        {
            var fi = new FileInfo(file);
            var ac = fi.GetAccessControl();
            var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var account = (NTAccount)sid.Translate(typeof(NTAccount));
            Log.Information("Granting full file permissions to current user");
            var fileAccessRule = new FileSystemAccessRule(account, FileSystemRights.FullControl, AccessControlType.Allow);
            
            ac.AddAccessRule(fileAccessRule);
            fi.SetAccessControl(ac);
        }
    }
}
