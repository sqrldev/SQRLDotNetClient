using Avalonia.Controls;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using SQRLDotNetClientUI.Models;
using Serilog;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.Generic;
using System.Windows.Input;
using SQRLCommonUI.AvaloniaExtensions;
using Avalonia;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ToolBox.Bridge;
namespace SQRLDotNetClientUI.ViewModels
{
    public class MainMenuViewModel : ViewModelBase
    {

        private bool _NewUpdateAvailable = true;
        public bool NewUpdateAvailable
        {
            get => _NewUpdateAvailable;
            set => this.RaiseAndSetIfChanged(ref _NewUpdateAvailable, value);
        }

        private string _siteUrl = "";
        public string SiteUrl
        {
            get => _siteUrl;
            set => this.RaiseAndSetIfChanged(ref _siteUrl, value);
        }

        private SQRLIdentity _currentIdentity;
        public SQRLIdentity CurrentIdentity
        {
            get => _currentIdentity;
            set
            {
                this.RaiseAndSetIfChanged(ref _currentIdentity, value);
                this.CurrentIdentityLoaded = (value != null);
            }
        }

        private bool _currentIdentityLoaded = false;
        public bool CurrentIdentityLoaded
        {
            get => _currentIdentityLoaded;
            set => this.RaiseAndSetIfChanged(ref _currentIdentityLoaded, value);
        }

        public String _IdentityName = "";
        public String IdentityName
        {
            get => _IdentityName;
            set => this.RaiseAndSetIfChanged(ref _IdentityName, value);
        }

        /// <summary>
        /// Gets a list of menu item objects representing the different
        /// languages that are supported by the app.
        /// </summary>
        public IReadOnlyList<LanguageMenuItem> LanguageMenuItems
        {
            get
            {
                List<LanguageMenuItem> items = new List<LanguageMenuItem>();
                foreach (var locInfo in LocalizationExtension.Localizations)
                {
                    object logo;
                    string prefix = string.Empty;

                    if (LocalizationExtension.CurrentLocalization == locInfo.Key)
                        logo = new CheckBox() { IsChecked = true, BorderThickness = new Thickness(0) };
                    else
                        logo = new Image() { Source = locInfo.Value.Image };

                    if (locInfo.Key == LocalizationExtension.DEFAULT_LOC)
                        prefix = _loc.GetLocalizationValue("DefaultLanguageMenuItemHeader") + " - ";

                    LanguageMenuItem item = new LanguageMenuItem()
                    {
                        Header = prefix + locInfo.Value.CultureInfo.DisplayName,
                        Command = ReactiveCommand.Create<string>(SelectLanguage),
                        CommandParameter = locInfo.Key,
                        Icon = logo
                    };

                    items.Add(item);
                }
                return items;
            }
            set { }
        }

        public AuthenticationViewModel AuthVM { get; set; }

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

            //Checks for New Version on Main Menu Start
            CheckForUpdates();
        }

        private void OnIdentityChanged(object sender, IdentityChangedEventArgs e)
        {
            this.IdentityName = e.IdentityName;
            this.CurrentIdentity = _identityManager.CurrentIdentity;
        }

        public void SelectLanguage(string language)
        {
            LocalizationExtension.CurrentLocalization = language;

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new MainMenuViewModel();
        }

        public void OnNewIdentityClick()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new NewIdentityViewModel();
        }

     

        public void ExportIdentity()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new ExportIdentityViewModel();
        }

        public void ImportIdentity()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new ImportIdentityViewModel();
        }

        public async void SwitchIdentity()
        {
            if (_identityManager.IdentityCount > 1)
            {
                SelectIdentityDialogView selectIdentityDialog = new SelectIdentityDialogView
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                await selectIdentityDialog.ShowDialog(_mainWindow);
            }
        }

        public void IdentitySettings()
        {
            Log.Information("Launching identity settings for identity id {IdentityUniqueId}",
                _identityManager.CurrentIdentityUniqueId);

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new IdentitySettingsViewModel();
        }

        public void ChangePassword()
        {
            Log.Information("Launching change password screen for identity id {IdentityUniqueId}",
                _identityManager.CurrentIdentityUniqueId);

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new ChangePasswordViewModel();
        }

        public void Exit()
        {
            _mainWindow.Close();

            Log.Information("App shutting down");
            ((IClassicDesktopStyleApplicationLifetime)App.Current.ApplicationLifetime)
                .Shutdown();
        }

        public async void DeleteIdentity()
        {

            var result = await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("DeleteIdentityMessageBoxTitle"),
                string.Format(_loc.GetLocalizationValue("DeleteIdentityMessageBoxText"), this.IdentityName, Environment.NewLine),
                MessageBoxSize.Medium, MessageBoxButtons.YesNo, MessageBoxIcons.QUESTION)
                .ShowDialog(this);

            if (result == MessagBoxDialogResult.YES)
            {
                _identityManager.DeleteCurrentIdentity();
            }
        }

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
        /// Checks for new version in github and enables the Alert Button
        /// </summary>
        public async void CheckForUpdates()
        {

            this.NewUpdateAvailable = await GitHubApi.GitHubHelper.CheckForUpdates();
        }


        /// <summary>
        /// Launches the installer to install a new update
        /// </summary>
        public async void InstallUpdate()
        {


            IBridgeSystem _bridgeSystem = BridgeSystem.Bash;
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string installer = GetInstallerByPlatform();
            if(File.Exists(Path.Combine(directory,installer)))
            {
                var tempFile = Path.GetTempPath();
                File.Copy(Path.Combine(directory, installer), Path.Combine(tempFile, Path.GetFileName(installer)), true);
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var _shell = new ShellConfigurator(_bridgeSystem);
                    Log.Information("Changing Executable File to be Executable a+x");
                    _shell.Term($"chmod a+x {Path.Combine(tempFile, Path.GetFileName(installer))}", Output.Internal);
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process proc = new Process();
                    proc.StartInfo.FileName = Path.Combine(tempFile, Path.GetFileName(installer));
                    proc.StartInfo.UseShellExecute = true;
                    proc.StartInfo.Verb = "runas";
                    proc.Start();
                }
                else
                    Process.Start(Path.Combine(tempFile, Path.GetFileName(installer)));

                this._mainWindow.Exit();
            }
            else
            {
                var result = await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("GenericQuestionTitle"),
                string.Format(_loc.GetLocalizationValue("MissingInstaller"), this.IdentityName, Environment.NewLine),
                MessageBoxSize.Medium, MessageBoxButtons.YesNo, MessageBoxIcons.QUESTION)
                .ShowDialog(this);
                if(result == MessagBoxDialogResult.YES)
                {
                    OpenUrl("https://github.com/sqrldev/SQRLDotNetClient/releases");
                    this._mainWindow.Exit();
                }
            }
        }

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

        public void RekeyIdentity()
        {

        
            Log.Information("Launching Rekey Identity for Identity: {IdentityUniqueId}",
                _identityManager.CurrentIdentityUniqueId);

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                new ReKeyViewModel();
        }
    }

    /// <summary>
    /// Represents a single menu item in the "languages" menu.
    /// </summary>
    public class LanguageMenuItem
    {
        public string Header;
        public ICommand Command;
        public object CommandParameter;
        public object Icon;
    }
}
