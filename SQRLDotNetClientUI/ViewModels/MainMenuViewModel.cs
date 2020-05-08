using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using SQRLDotNetClientUI.Models;
using Serilog;
using Avalonia.Controls.ApplicationLifetimes;
using SQRLCommonUI.AvaloniaExtensions;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ToolBox.Bridge;
using System.Threading.Tasks;
using Avalonia.Controls;
using SQRLCommonUI.Models;
using SQRLDotNetClientUI.DB.DBContext;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// A view model representing the app's main screen.
    /// </summary>
    public class MainMenuViewModel : ViewModelBase, ILocalizable
    {
        private bool _newUpdateAvailable = false;
        private string _siteUrl = "";
        private SQRLIdentity _currentIdentity;
        private bool _currentIdentityLoaded = false;
        public String _IdentityName = "";

        /// <summary>
        /// Gets or sets a value indicating whether there is a new app update
        /// available on Github.
        /// </summary>
        public bool NewUpdateAvailable
        {
            get => _newUpdateAvailable;
            set => this.RaiseAndSetIfChanged(ref _newUpdateAvailable, value);
        }

        /// <summary>
        /// Gets or sets the full site authentication URL.
        /// </summary>
        public string SiteUrl
        {
            get => _siteUrl;
            set => this.RaiseAndSetIfChanged(ref _siteUrl, value);
        }

        /// <summary>
        /// Gets or sets the currently active identity.
        /// </summary>
        public SQRLIdentity CurrentIdentity
        {
            get => _currentIdentity;
            set
            {
                this.RaiseAndSetIfChanged(ref _currentIdentity, value);
                this.CurrentIdentityLoaded = (value != null);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether there is an identity
        /// loaded/available or not.
        /// </summary>
        public bool CurrentIdentityLoaded
        {
            get => _currentIdentityLoaded;
            set => this.RaiseAndSetIfChanged(ref _currentIdentityLoaded, value);
        }

        /// <summary>
        /// Gets or sets the currently loaded identity's name.
        /// </summary>
        public String IdentityName
        {
            get => _IdentityName;
            set => this.RaiseAndSetIfChanged(ref _IdentityName, value);
        }

        /// <summary>
        /// Gets or sets the view model for the authentication screen.
        /// </summary>
        public AuthenticationViewModel AuthVM { get; set; }

        /// <summary>
        /// Creates a new <c>MainMenuViewModel</c> instance, performs some
        /// initialization tasks and checks for new app updates.
        /// </summary>
        public MainMenuViewModel()
        {
            this.Title = _loc.GetLocalizationValue("MainWindowTitle");
            this.CurrentIdentity = _identityManager.CurrentIdentity;
            this.IdentityName = this.CurrentIdentity?.IdentityName;

            _identityManager.IdentityChanged += OnIdentityChanged;

            string[] commandLine = Environment.CommandLine.Split(" ");
            if (commandLine.Length > 1)
            {

                if (Uri.TryCreate(commandLine[1], UriKind.Absolute, out Uri result) && this.CurrentIdentity != null)
                {
                    AuthenticationViewModel authView = new AuthenticationViewModel(result);
                    _mainWindow.Height = 300;
                    _mainWindow.Width = 400;
                    AuthVM = authView;
                }
            }
            else
            {
                _mainWindow.Height = 450;
                _mainWindow.Width = 400;
            }

            //Checks for new version on main menu start
            CheckForUpdates();
        }

        /// <summary>
        /// This event handler gets called when the currently active identity changes.
        /// </summary>
        private void OnIdentityChanged(object sender, IdentityChangedEventArgs e)
        {
            this.IdentityName = e.IdentityName;
            this.CurrentIdentity = _identityManager.CurrentIdentity;
        }

        /// <summary>
        /// Sets the given <paramref name="language"/> as the active language in the 
        /// <c>LocalizationExtension</c> and reloads the app's main screen for the
        /// language change to take effect.
        /// </summary>
        /// <param name="language">The language/localization code to set active (e.g. "en-US").</param>
        public void SelectLanguage(string language)
        {
            if (LocalizationExtension.CurrentLocalization == language) 
                return;

            LocalizationExtension.CurrentLocalization = language;

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new MainMenuViewModel();
        }

        /// <summary>
        /// This event handler gets called when the "New Identity" button or
        /// menu item is clicked.
        /// </summary>
        public void OnNewIdentityClick()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new NewIdentityViewModel();
        }

        /// <summary>
        /// This event handler gets called when the "Export Identity" button or
        /// menu item is clicked.
        /// </summary>
        public void ExportIdentity()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new ExportIdentityViewModel();
        }

        /// <summary>
        /// This event handler gets called when the "Import Identity" button or
        /// menu item is clicked.
        /// </summary>
        public void ImportIdentity()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new ImportIdentityViewModel();
        }

        /// <summary>
        /// Opens the identity selection dialog.
        /// </summary>
        public void SwitchIdentity()
        {
            if (_identityManager.IdentityCount > 1)
            {
                new SelectIdentityViewModel().ShowDialog(this);
            }
        }

        /// <summary>
        /// This event handler gets called when the "Identity Settings" button or
        /// menu item is clicked.
        /// </summary>
        public void IdentitySettings()
        {
            Log.Information("Launching identity settings for identity id {IdentityUniqueId}",
                _identityManager.CurrentIdentityUniqueId);

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new IdentitySettingsViewModel();
        }

        /// <summary>
        /// This event handler gets called when the "Change Password" button or
        /// menu item is clicked.
        /// </summary>
        public void ChangePassword()
        {
            Log.Information("Launching change password screen for identity id {IdentityUniqueId}",
                _identityManager.CurrentIdentityUniqueId);

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new ChangePasswordViewModel();
        }

        /// <summary>
        /// This event handler gets called when the "About" menu item is clicked.
        /// </summary>
        public void About()
        {
            Log.Information("Launching about screen");

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new AboutViewModel();
        }

        /// <summary>
        /// This event handler gets called when the "Exit" menu item is clicked.
        /// It shuts down the application.
        /// </summary>
        public void Exit()
        {
            _mainWindow.Close();

            ((IClassicDesktopStyleApplicationLifetime)App.Current.ApplicationLifetime)
                .Shutdown();
        }

        /// <summary>
        /// This event handler gets called when the "Delete Identity" button or
        /// menu item is clicked.
        /// </summary>
        public async void DeleteIdentity()
        {
            var result = await new MessageBoxViewModel(_loc.GetLocalizationValue("DeleteIdentityMessageBoxTitle"),
                string.Format(_loc.GetLocalizationValue("DeleteIdentityMessageBoxText"), this.IdentityName, Environment.NewLine),
                MessageBoxSize.Medium, MessageBoxButtons.YesNo, MessageBoxIcons.QUESTION)
                .ShowDialog(this);

            if (result == MessagBoxDialogResult.YES)
            {
                _identityManager.DeleteCurrentIdentity();
            }
        }

        /// <summary>
        /// This event handler gets called when the "Rekey Identity" button or
        /// menu item is clicked.
        /// </summary>
        public void RekeyIdentity()
        {
            Log.Information("Launching Rekey Identity for Identity: {IdentityUniqueId}",
                _identityManager.CurrentIdentityUniqueId);

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new ReKeyViewModel();
        }

        /// <summary>
        /// This event handler gets called when the app's "Settings" menu item is clicked.
        /// </summary>
        public void AppSettings()
        {
            Log.Information("Launching app settings screen");

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new AppSettingsViewModel();
        }

        /// <summary>
        /// Helper method for testing authentication without invoking a "sqrl://" link.
        /// </summary>
        public void Login()
        {
            if (!string.IsNullOrEmpty(this.SiteUrl) && this.CurrentIdentity != null)
            {
                if (Uri.TryCreate(this.SiteUrl, UriKind.Absolute, out Uri result))
                {
                    AuthenticationViewModel authView = new AuthenticationViewModel(result);
                    _mainWindow.Height = 300;
                    _mainWindow.Width = 400;
                    this.AuthVM = authView;
                    ((MainWindowViewModel)_mainWindow.DataContext).Content = AuthVM;
                }
            }
        }

        /// <summary>
        /// Checks for new version in Github and enables the alert button.
        /// </summary>
        /// <param name="userInitiated">Set to <c>true</c> if the update check was
        /// manually initiated by the user to trigger a UI response.</param>
        public async void CheckForUpdates(bool userInitiated = false)
        {
            if (!userInitiated)
            {
                TimeSpan timeSinceLastUpdate = DateTime.Now - App.LastUpdateCheck;
                if (timeSinceLastUpdate < App.MinTimeBetweenUpdateChecks) return;
            }
            else
            {
                Log.Information("User initiated update check");
            }

            this.NewUpdateAvailable = await GitHubApi.GitHubHelper.CheckForUpdates(
                Assembly.GetExecutingAssembly().GetName().Version);

            App.LastUpdateCheck = DateTime.Now;

            if (userInitiated && !this.NewUpdateAvailable)
            {
                await new MessageBoxViewModel(_loc.GetLocalizationValue("CheckForUpdates"),
                    _loc.GetLocalizationValue("NoUpdateAvailable"),
                    MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.OK)
                    .ShowDialog(this);
            }
        }

        /// <summary>
        /// Launches the installer to install a new update.
        /// </summary>
        public async void InstallUpdate()
        {
            Log.Information("User initiated installation of update");

            var installPathArgument = $"\"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\"";
            bool success = await RunInstaller(installPathArgument);
            if (success)
            {
                Log.CloseAndFlush();
                _mainWindow.Exit();
            }
        }

        /// <summary>
        /// Launches the installer in uninstall mode.
        /// </summary>
        private async void Uninstall()
        {
            Log.Information("User initiated uninstall request from client");

            var result = await new MessageBoxViewModel(_loc.GetLocalizationValue("GenericQuestionTitle"),
                string.Format(_loc.GetLocalizationValue("ReallyUninstallQuestion"), this.IdentityName, Environment.NewLine),
                MessageBoxSize.Medium, MessageBoxButtons.YesNo, MessageBoxIcons.QUESTION)
                .ShowDialog(this);

            if (result != MessagBoxDialogResult.YES) return;

            var uninstallArgument = "-uninstall";
            bool success = await RunInstaller(uninstallArgument);
            if (success)
            {
                Log.CloseAndFlush();
                _mainWindow.Exit();
            }
        }

        /// <summary>
        /// Launches the installer binary, passing in the provided <paramref name="arguments"/>.
        /// </summary>
        /// <param name="arguments">The command line arguments to pass to the installer.</param>
        private async Task<bool> RunInstaller(string arguments)
        {
            IBridgeSystem _bridgeSystem = BridgeSystem.Bash;
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string installer = GetInstallerByPlatform();
            if (File.Exists(Path.Combine(directory, installer)))
            {
                var tempFile = Path.GetTempPath();
                File.Copy(Path.Combine(directory, installer), Path.Combine(tempFile, Path.GetFileName(installer)), true);
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var _shell = new ShellConfigurator(_bridgeSystem);
                    Log.Information("Changing executable file to be executable a+x");
                    _shell.Term($"chmod a+x {Path.Combine(tempFile, Path.GetFileName(installer))}", Output.Internal);
                }

                Log.Information("Starting Installer");
                Process proc = new Process();
                proc.StartInfo.FileName = Path.Combine(tempFile, Path.GetFileName(installer));
                proc.StartInfo.Arguments = arguments;
                Log.Information($"Installer Location: {proc.StartInfo.FileName}");
                Log.Information($"Installer Arguments: {proc.StartInfo.Arguments}");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    proc.StartInfo.UseShellExecute = true;
                    proc.StartInfo.Verb = "runas";
                }
                proc.Start();
                return true;
            }
            else
            {
                Log.Information("Installer binary missing, asking user to download");
                var result = await new MessageBoxViewModel(_loc.GetLocalizationValue("GenericQuestionTitle"),
                    string.Format(_loc.GetLocalizationValue("MissingInstaller"), this.IdentityName, Environment.NewLine),
                    MessageBoxSize.Medium, MessageBoxButtons.YesNo, MessageBoxIcons.QUESTION)
                    .ShowDialog(this);

                if (result == MessagBoxDialogResult.YES)
                {
                    Log.Information("Sending user to Github");
                    OpenUrl("https://github.com/sqrldev/SQRLDotNetClient/releases");
                    return true;
                }
                else
                {
                    Log.Information("User declined downloading installer.");
                }

                return false;
            }
        }

        /// <summary>
        /// Opens the specified <paramref name="url"/> in the standard browser.
        /// </summary>
        /// <param name="url">The link/URL to open.</param>
        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Returns the name of the installer corresponding to the current platform.
        /// </summary>
        private string GetInstallerByPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "SQRLPlatformAwareInstaller_win.exe";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "SQRLPlatformAwareInstaller_osx";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "SQRLPlatformAwareInstaller_linux";

            return "";
        }

        /// <summary>
        /// Allows a user to pick and load or import a sqrl.db file to be used to store application identities and settings
        /// </summary>
        private async void ImportDB()
        {
            Log.Information("User chose to Import New DB File");
            var ofdDB = new OpenFileDialog();
            ofdDB.Title = _loc.GetLocalizationValue("DbFileTitle");
            FileDialogFilter fdf = new FileDialogFilter();
            fdf.Extensions.Add("db");
            fdf.Name = "sqrl";
            ofdDB.Filters.Add(fdf);
            var result = await ofdDB.ShowAsync(_mainWindow);
            if (result != null)
            {
                Log.Information($"Chose File {result[0]}");
                if (string.Compare(PathConf.FullClientDbPath.Trim(), result[0].Trim(), true) == 0)
                {
                    Log.Error("The chosen file is the same as the currently loaded Db File, Abort");
                    await new MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"), _loc.GetLocalizationValue("DbFileAlreadyLoaded"), messageBoxIcon: MessageBoxIcons.ERROR).ShowDialog(this);
                }
                else
                {
                    var diagResult = await new MessageBoxViewModel(_loc.GetLocalizationValue("GenericQuestionTitle"),
                           string.Format(_loc.GetLocalizationValue("DbMoveQuestion"), PathConf.FullClientDbPath, result[0], Path.Combine(PathConf.DefaultClientDBPath, PathConf.DBNAME)),
                           MessageBoxSize.Medium, MessageBoxButtons.Custom, MessageBoxIcons.QUESTION, new MessageBoxCustomButton [] {
                                                                                              new MessageBoxCustomButton (_loc.GetLocalizationValue("DbMoveAndLoad"),MessagBoxDialogResult.CUSTOM1,true),
                                                                                              new MessageBoxCustomButton (_loc.GetLocalizationValue("DbJustLoad"),MessagBoxDialogResult.CUSTOM2,false),
                                                                                              new MessageBoxCustomButton (_loc.GetLocalizationValue("BtnCancel"),MessagBoxDialogResult.CANCEL,false)})

                           .ShowDialog(this);
                    switch (diagResult)
                    {
                        // Move and Load
                        case MessagBoxDialogResult.CUSTOM1:
                            {
                                string newDbLocation = Path.Combine(PathConf.DefaultClientDBPath, PathConf.DBNAME);
                                Log.Information($"User chose to move Db file from:{result[0]} to {newDbLocation}");
                                try
                                {
                                    SQRLDBContext.DisposeDB();
                                    File.Move(result[0], newDbLocation, true);
                                    PathConf.ClientDBPath = Path.GetDirectoryName(newDbLocation);
                                    Models.AppSettings.Instance.Initialize();
                                    _identityManager.Initialize();
                                    ((MainWindowViewModel)_mainWindow.DataContext).Content = new MainMenuViewModel();
                                }
                                catch(Exception err)
                                {
                                    Log.Error($"Error moving DB File Error: {err.ToString()}");
                                    await new MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"), _loc.GetLocalizationValue("DbMoveError"), messageBoxIcon: MessageBoxIcons.ERROR).ShowDialog(this);                                    
                                }
                            }
                            break;
                        //Just Load
                        case MessagBoxDialogResult.CUSTOM2:
                            {
                                Log.Information($"User chose to load new Db File in Place");
                                PathConf.ClientDBPath = Path.GetDirectoryName(result[0]);
                                SQRLDBContext.DisposeDB();
                                Models.AppSettings.Instance.Initialize();
                                _identityManager.Initialize();
                                ((MainWindowViewModel)_mainWindow.DataContext).Content = new MainMenuViewModel();
                            }
                            break;
                    }
                }
            }
            else
                Log.Information("No Db File Chosen");
        }
    }
}
