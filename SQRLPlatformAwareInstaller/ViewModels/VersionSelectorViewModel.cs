using ReactiveUI;
using System;
using System.Net;
using System.Linq;
using System.IO;
using Avalonia.Controls;
using Microsoft.Win32;
using Serilog;
using System.Runtime.InteropServices;
using SQRLCommon.Models;
using SQRLPlatformAwareInstaller.Models;
using SQRLPlatformAwareInstaller.Platform;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Diagnostics;
using Avalonia.Media;
using System.Collections.Generic;

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
                SelectedReleaseChanged(); 
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
            Init(updateMode: false);
            GetReleasesAsync();
        }

        /// <summary>
        /// Creates a new instance and immediately performs an update using the specified parameters.
        /// </summary>
        /// <param name="installArchivePath">The path to the update zip file.</param>
        /// <param name="versionTag">The version tag corresponding to the provided update zip file.</param>
        public VersionSelectorViewModel(string installArchivePath, string versionTag)
        {
            Log.Information($"Version selection screen launched with existing update zip file path \"{installArchivePath}\"");
            Log.Information($"Initiating installation of existing update package");
            Init(updateMode: true);
            ApplyUpdate(installArchivePath, versionTag);
        }

        /// <summary>
        /// This is just an async wrapper around <see cref="GetReleases(bool)"/> so
        /// that it can be called from the constructor.
        /// </summary>
        private async void GetReleasesAsync()
        {
            await GetReleases();
        }

        /// <summary>
        /// Performs initialization tasks.
        /// </summary>
        /// <param name="updateMode">Set to <c>true</c> if we are in update mode
        /// and don't want to trigger release updates when pre-releases are 
        /// enabled/disabled.</param>
        private void Init(bool updateMode)
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

            if (!updateMode)
            {
                this.WhenAnyValue(x => x.EnablePreReleases)
                .Subscribe(async x => await GetReleases());
            }

            // Initialize the web client we use to download stuff from Github
            _webClient = new WebClient();
            _webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(
                System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            _webClient.Headers.Add("User-Agent", GithubHelper.SQRLInstallerUserAgent);

            // Check for existing sqrl:// scheme registration on Windows and warn
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
        /// Installs the pre-downloaded update package given in <paramref name="installArchivePath"/>.
        /// </summary>
        /// <param name="installArchivePath">The path to an existing update zip file.</param>
        /// <param name="versionTag">The version tag corresponding to the provided update zip file.</param>
        private async void ApplyUpdate(string installArchivePath, string versionTag)
        {
            // Enable all releases
            this.EnablePreReleases = true;

            try
            {
                // Try to fetch releases from file first, only if it's not available,
                // fall back to fetching them online.
                await GetReleases(fromFile: true);
                if (!HasReleases) await GetReleases(fromFile: false);

                // Disable "Install" button
                this.CanInstall = false;

                // Try pre-selecting the release from what was passed in the -v command 
                // line switch, so that it matches what was initially chosen by the user
                if (HasReleases)
                {
                    var release = this.Releases.Where(x => x.tag_name == versionTag)?.First();
                    if (this.SelectedRelease == null || this.SelectedRelease?.tag_name != release.tag_name)
                    {
                        this.SelectedRelease = release;
                    }

                    // Now start the actual installation using the zip archive
                    // and version tag passed in.
                    await InstallOnPlatform(installArchivePath, versionTag);
                }
                else
                {
                    Log.Error("Looks like we have not found any releases - aborting update!");
                    SetErrorStatus();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error continuing installation with new intaller:\r\n{ex}");
                SetErrorStatus();
            }

            return;
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

            Log.Information($"Found {this.Releases?.Count()} releases");
            this.SelectedRelease = this.Releases.OrderByDescending(r => r.published_at).FirstOrDefault();
        }

        /// <summary>
        /// Sets the download size and URL for the selected release.
        /// </summary>
        public void SelectedReleaseChanged()
        {
            if (SelectedRelease == null) return;

            var downloadInfo = GithubHelper.GetDownloadInfoForAsset(SelectedRelease);
            this.DownloadSize = downloadInfo?.DownloadSize;
            this._downloadUrl = downloadInfo?.DownloadUrl;

            Log.Information($"Selected release changed to {SelectedRelease.tag_name}");
            Log.Information($"Current release asset download size is : {this.DownloadSize} MB");
            Log.Information($"Current release asset download URL is : {this._downloadUrl}");
        }

        /// <summary>
        /// Downloads the selected release file to a temporary file and starts
        /// the installation process.
        /// </summary>
        public async void DownloadInstall()
        {
            if (string.IsNullOrEmpty(this._downloadUrl)) return;

            this.InstallStatus = _loc.GetLocalizationValue("InstallStatusDownloading");
            this.CanInstall = false;

            var progress = new Progress<KeyValuePair<int, string>>(x =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    this.DownloadPercentage = x.Key;
                    this.InstallStatus = x.Value;
                });
            });

            this._downloadedFileName = await GithubHelper.DownloadRelease(this.SelectedRelease, progress);
            Log.Information($"Download completed, file path is \"{this._downloadedFileName}\"");
            
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
