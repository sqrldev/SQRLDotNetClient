using ReactiveUI;
using System;
using System.Net;
using System.Linq;
using System.IO;
using Avalonia.Controls;
using Microsoft.Win32;
using GitHubApi;
using Serilog;
using System.Runtime.InteropServices;
using SQRLCommonUI.Models;
using SQRLPlatformAwareInstaller.Models;
using SQRLPlatformAwareInstaller.Platform;
using System.Threading.Tasks;
using Avalonia.Threading;

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
        private string _executablePath = "";
        private string _installStatus = "";
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

            this.InstallationPath = Environment.GetCommandLineArgs().Length > 1 ?
                Environment.GetCommandLineArgs()[1] : 
                PathConf.ClientInstallPath;

            _installer = Activator.CreateInstance(
                Implementation.ForType<IInstaller>()) as IInstaller;

            _executablePath = _installer.GetClientExePath(this.InstallationPath);

            _webClient = new WebClient();
            _webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            _webClient.Headers.Add("User-Agent", GitHubHelper.SQRLInstallerUserAgent);

            GetReleases();
        }

        /// <summary>
        /// Fetches available releases from Github.
        /// </summary>
        private async void GetReleases()
        {
            this.Releases = await GitHubHelper.GetReleases(this.EnablePreReleases);
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

            Dispatcher.UIThread.Post(() =>
            {
                this.InstallStatus = _loc.GetLocalizationValue("InstallStatusInstalling");
                this.DownloadPercentage = 0;
                this.IsProgressIndeterminate = true;
            });

            await InstallOnPlatform(this._downloadedFileName);

            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                new InstallationCompleteViewModel(Path.Combine(this._executablePath));
        }

        /// <summary>
        /// Installs the contents of the archive specified by<paramref name="downloadedFileName"/> 
        /// according to the detected platform.
        /// </summary>
        /// <param name="downloadedFileName">The downloaded application files to install.</param>
        private async Task InstallOnPlatform(string downloadedFileName)
        {
            // Write the installation path to the config file so that
            // we can locate the installation later
            Log.Information($"Writing installation path {this.InstallationPath} to config file");
            PathConf.ClientInstallPath = this.InstallationPath;

            // Perform the actual installation
            Log.Information($"Launching installation");
            await _installer.Install(downloadedFileName, this.InstallationPath, this.SelectedRelease.tag_name);
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
