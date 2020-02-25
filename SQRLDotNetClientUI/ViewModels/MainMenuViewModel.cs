using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.AvaloniaExtensions;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;

namespace SQRLDotNetClientUI.ViewModels
{
    public class MainMenuViewModel : ViewModelBase
    {
        private IdentityManager _identityManager = IdentityManager.Instance;
        private LocalizationExtension _loc = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService;
        private MainWindow _mainWindow = AvaloniaLocator.Current.GetService<MainWindow>();

        private string _siteUrl = "";
        public string SiteUrl 
        { 
            get => _siteUrl; 
            set => this.RaiseAndSetIfChanged(ref _siteUrl, value); 
        }

        public SQRL sqrlInstance { get; set; }

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

        public AuthenticationViewModel AuthVM { get; set; }
   
        public MainMenuViewModel(SQRL sqrlInstance)
        {
            this.sqrlInstance = sqrlInstance;
            Init();

            string[] commandLine = Environment.CommandLine.Split(" ");
            if(commandLine.Length>1)
            {

               if (Uri.TryCreate(commandLine[1], UriKind.Absolute, out Uri result) && this.CurrentIdentity!=null)
                {
                    AuthenticationViewModel authView = new AuthenticationViewModel(this.sqrlInstance, this.CurrentIdentity, result);
                    _mainWindow.Height = 300;
                    _mainWindow.Width = 400;
                    AuthVM = authView;
                }
            }
        }

        public MainMenuViewModel()
        {
            Init();
        }

        private void Init()
        {
            this.Title = _loc.GetLocalizationValue("MainWindowTitle");
            this.CurrentIdentity = _identityManager.CurrentIdentity;
            this.IdentityName = this.CurrentIdentity?.IdentityName;

            _identityManager.IdentityChanged += OnIdentityChanged;
        }

        private void OnIdentityChanged(object sender, IdentityChangedEventArgs e)
        {
            this.IdentityName = e.IdentityName;
            this.CurrentIdentity = _identityManager.CurrentIdentity;
        }
        
        public void OnNewIdentityClick()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                new NewIdentityViewModel(this.sqrlInstance);
        }

        public void ExportIdentity()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                new ExportIdentityViewModel(this.sqrlInstance);
        }

        public void ImportIdentity()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                new ImportIdentityViewModel(this.sqrlInstance);
        }

        public async void SwitchIdentity()
        {
            if (_identityManager.IdentityCount > 1)
            {
                SelectIdentityDialogView selectIdentityDialog = new SelectIdentityDialogView();
                selectIdentityDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                await selectIdentityDialog.ShowDialog(_mainWindow);
            }
        }

        public void IdentitySettings()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                new IdentitySettingsViewModel(this.sqrlInstance);
        }

        public void Exit()
        {
            _mainWindow.Close();
        }

        public async void DeleteIdentity()
        {
            var msgBox = MessageBoxManager.GetMessageBoxStandardWindow(
                    _loc.GetLocalizationValue("DeleteIdentityMessageBoxTitle"),
                    string.Format(_loc.GetLocalizationValue("DeleteIdentityMessageBoxText"), this.IdentityName, Environment.NewLine),
                    ButtonEnum.YesNo,
                    Icon.Warning);

            var result = await msgBox.ShowDialog(_mainWindow);

            if (result == ButtonResult.Yes)
            {
                _identityManager.DeleteCurrentIdentity();
            }
        }

        public void Login()
        {
            if(!string.IsNullOrEmpty(this.SiteUrl) && this.CurrentIdentity!=null)
            {
                if (Uri.TryCreate(this.SiteUrl, UriKind.Absolute, out Uri result))
                {
                    AuthenticationViewModel authView = new AuthenticationViewModel(this.sqrlInstance, this.CurrentIdentity, result);
                    _mainWindow.Height = 300;
                    _mainWindow.Width = 400;
                    this.AuthVM = authView;
                    ((MainWindowViewModel)_mainWindow.DataContext).Content = AuthVM;
                }
            }
        }
    }
}
