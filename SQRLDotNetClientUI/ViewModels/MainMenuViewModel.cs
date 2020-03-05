using Avalonia.Controls;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using SQRLDotNetClientUI.Models;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using Serilog;
using System.Threading;
using System.Linq;

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

        public AuthenticationViewModel AuthVM { get; set; }
   
        public MainMenuViewModel()
        {
            this.Title = _loc.GetLocalizationValue("MainWindowTitle");
            this.CurrentIdentity = _identityManager.CurrentIdentity;
            this.IdentityName = this.CurrentIdentity?.IdentityName;

            _identityManager.IdentityChanged += OnIdentityChanged;

            string[] commandLine = Environment.CommandLine.Split(" ");
            if(commandLine.Length>1)
            {

               if (Uri.TryCreate(commandLine[1], UriKind.Absolute, out Uri result) && this.CurrentIdentity!=null)
                {
                    AuthenticationViewModel authView = new AuthenticationViewModel(result);
                    _mainWindow.Height = 300;
                    _mainWindow.Width = 400;
                    AuthVM = authView;
                }
            }

            TestQuickPass();   
        }

        private async void TestQuickPass()
        {
            QuickPassManager quickPass = QuickPassManager.Instance;

            byte[] imk = Sodium.SodiumCore.GetRandomBytes(32);
            var id = _identityManager.CurrentIdentity;
            id.Block1.PwdTimeoutMins = 1;

            quickPass.QuickPassCleared += (s, e) => { };

            await quickPass.SetQuickPass("test12345678", imk, id);
            bool isSet = quickPass.HasQuickPass();
            byte[] imk2 = await quickPass.GetQuickPassDecryptedImk("test");

            bool same = (imk.SequenceEqual(imk2));
            bool didClear = quickPass.ClearQuickPass("abc");
            Thread.Sleep(2000);
            quickPass.ClearQuickPass(_identityManager.CurrentIdentityUniqueId);
        }

        private void OnIdentityChanged(object sender, IdentityChangedEventArgs e)
        {
            this.IdentityName = e.IdentityName;
            this.CurrentIdentity = _identityManager.CurrentIdentity;
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
                SelectIdentityDialogView selectIdentityDialog = new SelectIdentityDialogView();
                selectIdentityDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
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
                    AuthenticationViewModel authView = new AuthenticationViewModel(result);
                    _mainWindow.Height = 300;
                    _mainWindow.Width = 400;
                    this.AuthVM = authView;
                    ((MainWindowViewModel)_mainWindow.DataContext).Content = AuthVM;
                }
            }
        }
    }
}
