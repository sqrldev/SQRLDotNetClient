using ReactiveUI;
using System;
using System.Net;
using System.Linq;
using System.IO;
using Avalonia.Controls;
using Microsoft.Win32;
using Serilog;
using System.Runtime.InteropServices;
using SQRLCommonUI.Models;
using SQRLPlatformAwareInstaller.Models;
using SQRLPlatformAwareInstaller.Platform;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Diagnostics;
using Avalonia.Media;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    /// <summary>
    /// A view model representing the version selection screen
    /// of the SQRL installer.
    /// </summary>
    public class VersionSelectorViewModel : ViewModelBase
    {
        private IInstaller _installer = null;
        private WebClient _webClient;
        private int _downloadPercentage;
        private string _downloadUrl = "";
        private string _installationPath;
        private string _warning = "";
        private string _installStatus = "";
        private IBrush _installStatusColor = Brushes.Black;
        private string _downloadedFileName;
        private GithubRelease[] _releases;
        private GithubRelease _selectedRelease;
        private decimal? _downloadSize;
        private bool _enablePreReleases = false;
        private bool _hasReleases = false;
        private bool _canInstall = false;
        private bool _isProgressIndeterminate = false;

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
            get { return this._installationPath; }
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
        /// Gets or sets the text color of the install status lavbel.
        /// </summary>
        public IBrush InstallStatusColor
        {
            get { return this._installStatusColor; }
            set
            {
                { this.RaiseAndSetIfChanged(ref _installStatusColor, value); }
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
            set 
            { 
                this.RaiseAndSetIfChanged(ref _selectedRelease, value); 
                SetDownloadSize(); 
            }
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
            set 
            { 
                this.RaiseAndSetIfChanged(ref _hasReleases, value);
                this.CanInstall = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the install button should be enabled.
        /// </summary>
        public bool CanInstall
        {
            get { return this._canInstall; }
            set { this.RaiseAndSetIfChanged(ref _canInstall, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the progress bar should be displayed
        /// as indeterminate.
        /// </summary>
        public bool IsProgressIndeterminate
        {
            get { return _isProgressIndeterminate; }
            set { this.RaiseAndSetIfChanged(ref _isProgressIndeterminate, value); }
        }

        /// <summary>
        /// Gets a value indicating whether it should be allowed for the user to change
        /// the installation path through the UI.
        /// </summary>
        public bool CanChangeInstallPath
        {
            get { return !RuntimeInformation.IsOSPlatform(OSPlatform.OSX); }
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
        /// Creates a new instance and performs some initializations.
        /// </summary>
        /// <param name="installArchivePath">The path to the update zip file.</param>
        /// <param name="versionTag">The version tag corresponding to the provided update zip file.</param>
        public VersionSelectorViewModel(string installArchivePath, string versionTag)
        {
            Log.Information($"Version selection screen launched with existing update zip file path \"{installArchivePath}\"");
            Log.Information($"Initiating installation of existing update package");
            Init(installArchivePath, versionTag);
        }

        /// <summary>
        /// Performs initialization tasks.
        /// </summary>
        /// <param name="installArchivePath">The path to an existing update zip file (optional).</param>
        /// <param name="versionTag">The version tag corresponding to the provided update zip file (optional).</param>
        private async void Init(string installArchivePath = null, string versionTag = null)
        {
            this.Title = _loc.GetLocalizationValue("TitleVersionSelector");         

            // Let's set the preset for the client installation path.
            // We first fetch the path from the config file, but if a valid
            // path was specified on the command line, it will overrule the
            // one from the config file!
            string installPath = PathConf.ClientInstallPath;
            if (!string.IsNullOrEmpty(CommandLineArgs.Instance.InstallPath) && 
                Directory.Exists(CommandLineArgs.Instance.InstallPath))
            {
                installPath = CommandLineArgs.Instance.InstallPath;
            }
            this.InstallationPath = installPath;

            // Create a platform-specific installer instance
            _installer = Activator.CreateInstance(
                Implementation.ForType<IInstaller>()) as IInstaller;

            // If we have an update zip file already, this means a previous
            // instance of the installer has already downloaded the update
            // from Github, and we just need to finish the installation.
            if (!string.IsNullOrEmpty(installArchivePath) &&
                !string.IsNullOrEmpty(versionTag))
            {
                // Enable all releases, since we don't know what the user did
                // in the "previous" installer run
                this.EnablePreReleases = true;

                try
                {
                    // Try to fetch releases from file first, only if it's not available,
                    // fall back to fetching them online.
                    await GetReleases(fromFile: true);
                    if (!HasReleases) await GetReleases(fromFile: false);

                    // Try pre-selecting the release from what was passed in the -v command 
                    // line switch, so that it matches what was initially chosen by the user
                    if (HasReleases)
                    {
                        this.SelectedRelease = this.Releases.Where(x => x.tag_name == versionTag)?.First();
                    }

                    // Now start the actual installation using the zip archive
                    // and version tag passed in.
                    await InstallOnPlatform(installArchivePath, versionTag);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error continuing installation with new intaller:\r\n{ex}");
                    SetErrorStatus();
                }

                return;
            }

            // If we get here, we need to let the user choose a release
            // and download and install it

            this.WhenAnyValue(x => x.EnablePreReleases)
                .Subscribe(async x => await GetReleases());

            // Initialize the web client we use to download stuff from Github
            _webClient = new WebClient();
            _webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(
                System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            _webClient.Headers.Add("User-Agent", GithubHelper.SQRLInstallerUserAgent);

            // Fetch releases from Github
            await GetReleases();
        }

        /// <summary>
        /// Fetches available releases from Github.
        /// </summary>
        /// <param name="fromFile">If set to <c>true</c>, the releases will not be fetched
        /// from Github, but instead be read from a local file.</param>
        private async Task GetReleases(bool fromFile = false)
        {
            this.Releases = await GithubHelper.GetReleases(this.EnablePreReleases, fromFile);
            this.HasReleases = this.Releases.Length > 0;
            if (!this.HasReleases) return;

            Log.Information($"Found {this.Releases?.Count()} Releases");
            this.SelectedRelease = this.Releases.OrderByDescending(r => r.published_at).FirstOrDefault();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log.Information($"We are on Windows, checking to see if an existing version of SQRL exists");
                if (Registry.ClassesRoot.OpenSubKey(@"sqrl") != null)
                {
                    Log.Information($"Display warning that we may overwrite an existing SQRL schema registration");
                    this.Warning = _loc.GetLocalizationValue("SchemaRegistrationWarning");
                }
            }
        }

        /// <summary>
        /// Sets the download size and URL for the selected release.
        /// </summary>
        public void SetDownloadSize()
        {
            if (SelectedRelease == null) return;

            var downloadInfo = _installer.GetDownloadInfoForAsset(SelectedRelease);
            this.DownloadSize = downloadInfo?.DownloadSize;
            this._downloadUrl = downloadInfo?.DownloadUrl;

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
            this.CanInstall = false;

            _webClient.DownloadProgressChanged += Wc_DownloadProgressChanged;
            _webClient.DownloadFileCompleted += Wc_DownloadFileCompleted;

            this._downloadedFileName = Path.GetTempFileName();
            Log.Information($"Temporary download file name: {_downloadedFileName}");
            _webClient.DownloadFileAsync(new Uri(this._downloadUrl), _downloadedFileName);
        }

        /// <summary>
        /// Event handler for the "download completed" event.
        /// </summary>
        private async void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Log.Information("Download completed");

            if (CommandLineArgs.Instance.Action == InstallerAction.Update)
            {
                // We're in "update" mode, so try to extract and launch the new installer binary from 
                // the downloaded update package, so that it can continue with the installation.
                // This way, we don't need to wait for the next release to benefit from enhancements
                // in the installer.
                try
                {
                    Log.Information("Extracting new installer from downloaded archive.");
                    var installerExeName = CommonUtils.GetInstallerByPlatform();
                    var tempDir = Path.Combine(Path.GetTempPath(), "SQRL", DateTime.Now.Ticks.ToString());
                    var installerTempFilePath = Path.Combine(tempDir, installerExeName);
                    Log.Information($"Temp file path for installer: \"{installerTempFilePath}\"");
                    var outFile = Path.Combine(tempDir, installerExeName);

                    Directory.CreateDirectory(tempDir);
                    Utils.ExtractSingleFile(this._downloadedFileName, null, installerExeName, outFile);
                    SystemAndShellUtils.SetExecutableBit(outFile);

                    Process process = new Process();
                    process.StartInfo.FileName = outFile;
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(outFile);
                    process.StartInfo.ArgumentList.Add($"-a Update");
                    process.StartInfo.ArgumentList.Add($"-z \"{_downloadedFileName}\"");
                    process.StartInfo.ArgumentList.Add($"-v \"{_selectedRelease?.tag_name}\"");
                    process.Start();

                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error extracting new installer from downloaded archive:\r\n{ex}");
                    Log.Error($"Continuing installation with current, outdated installer");
                }
            }

            await InstallOnPlatform(this._downloadedFileName, this.SelectedRelease?.tag_name);
        }

        /// <summary>
        /// Installs the contents of the archive specified by<paramref name="downloadedFileName"/> 
        /// according to the detected platform.
        /// </summary>
        /// <param name="downloadedFileName">The downloaded application files to install.</param>
        /// <param name="versionTag">The version tag corresponding to the selected release or
        /// the provided update zip file.</param>
        private async Task InstallOnPlatform(string downloadedFileName, string versionTag)
        {
            // Set the progress bar to "indeterminate"
            Dispatcher.UIThread.Post(() =>
            {
                this.DownloadPercentage = 0;
                this.IsProgressIndeterminate = true;
            });

            // Write the installation path to the config file so that
            // we can locate the installation later
            Log.Information($"Writing installation path to config file: \"{this.InstallationPath}\"");
            PathConf.ClientInstallPath = this.InstallationPath;

            // Check if client is running and kill it if necessary
            Log.Information($"Checking if client is running");
            // On macOS, process names are truncated to 15 chararcters, 
            // so we cannot match the full process name
            var procName = Path.GetFileNameWithoutExtension(_installer.GetClientExePath(this.InstallationPath))
                .Substring(0, 15);
            Process[] processes = Process.GetProcesses()
                .Where(x => x.ProcessName.StartsWith(procName)).ToArray();
            if (processes.Length > 0)
            {
                var clientProcess = processes[0];

                Log.Warning($"Client is running, trying to kill it");
                clientProcess.Kill();

                // Since the below call to "WaitForExit()" could potentially block
                // forever if killing the client fails for some reason, we better
                // give an indication of what's going on in the UI and hopefully have the
                // user help out closing the client if our programmatic approach fails.
                Dispatcher.UIThread.Post(() => this.InstallStatus = 
                    _loc.GetLocalizationValue("InstallStatusWaitingForClientExit"));

                await Task.Run(() => clientProcess.WaitForExit());

                Log.Information($"Client exited, continuing");
            }
            else 
                Log.Information($"Client not running, continuing");

            // Set status "installing" in UI
            Dispatcher.UIThread.Post(() => this.InstallStatus = 
                _loc.GetLocalizationValue("InstallStatusInstalling"));

            // Perform the actual installation
            Log.Information($"Launching installation");
            try
            {
                await _installer.Install(downloadedFileName, this.InstallationPath, versionTag);
            }
            catch (Exception ex)
            {
                Log.Error($"Error during installation:\r\n{ex}");
                SetErrorStatus();
                return;
            }

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new InstallationCompleteViewModel(_installer.GetClientExePath(this.InstallationPath));
        }

        /// <summary>
        /// Displays a red error status message in the UI, asking the user to
        /// check the log file and provide information to the developers.
        /// </summary>
        private void SetErrorStatus()
        {
            Dispatcher.UIThread.Post(() =>
            {
                this.InstallStatusColor = Brushes.Red;
                this.InstallStatus = _loc.GetLocalizationValue("InstallStatusError");
                this.DownloadPercentage = 100;
                this.IsProgressIndeterminate = false;
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
                    {
                        var lastDir = Path.GetFileName(result.TrimEnd(Path.DirectorySeparatorChar));
                        if (lastDir != "SQRL")
                        {
                            result = Path.Combine(result, "SQRL");
                        }

                        this.InstallationPath = result;
                    }
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
    }
}
