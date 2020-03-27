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
using SQRLDotNetClientUI.AvaloniaExtensions;
using Avalonia;

namespace SQRLDotNetClientUI.ViewModels
{
    public class MainMenuViewModel : ViewModelBase
    {
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
        }

        private void OnIdentityChanged(object sender, IdentityChangedEventArgs e)
        {
            this.IdentityName = e.IdentityName;
            this.CurrentIdentity = _identityManager.CurrentIdentity;
        }

        public void SelectLanguage(string language)
        {
            LocalizationExtension.CurrentLocalization = language;

            //MainMenuView mmv = 
            //MenuItem langMenu = mmv.FindControl<MenuItem>("menuLanguage");
            //langMenu.Items = null;
            //_mainWindow.DataContext = new MainWindowViewModel();

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

            var result = await new Views.MessageBox(_loc.GetLocalizationValue("DeleteIdentityMessageBoxTitle"),
                string.Format(_loc.GetLocalizationValue("DeleteIdentityMessageBoxText"), this.IdentityName, Environment.NewLine),
                MessageBoxSize.Medium, MessageBoxButtons.YesNo, MessageBoxIcons.QUESTION)
                .ShowDialog<MessagBoxDialogResult>(_mainWindow);

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
