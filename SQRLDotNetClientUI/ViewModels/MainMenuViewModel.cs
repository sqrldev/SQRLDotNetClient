using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using SQRLDotNetClientUI.Models;

namespace SQRLDotNetClientUI.ViewModels
{
    public class MainMenuViewModel : ViewModelBase
    {
        private IdentityManager _identityManager = IdentityManager.Instance;

        private string _siteUrl = "";
        public string SiteUrl { get => _siteUrl; set => this.RaiseAndSetIfChanged(ref _siteUrl, value); }
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
        public bool CurrentIdentityLoaded { get => _currentIdentityLoaded; set => this.RaiseAndSetIfChanged(ref _currentIdentityLoaded, value); }

        public String _IdentityName = "";
        public String IdentityName { get => _IdentityName; set => this.RaiseAndSetIfChanged(ref _IdentityName, value); }

        public AuthenticationViewModel AuthVM { get; set; }
   
        public MainMenuViewModel(SQRL sqrlInstance)
        {
            this.Title = "SQRL Client";
            this.sqrlInstance = sqrlInstance;

            this.CurrentIdentity = _identityManager.CurrentIdentity;
            this.IdentityName = this.CurrentIdentity?.IdentityName;

            _identityManager.IdentityChanged += OnIdentityChanged;

            string[] commandLine = Environment.CommandLine.Split(" ");
            if(commandLine.Length>1)
            {

               if (Uri.TryCreate(commandLine[1], UriKind.Absolute, out Uri result) && this.CurrentIdentity!=null)
                {
                    AuthenticationViewModel authView = new AuthenticationViewModel(this.sqrlInstance, this.CurrentIdentity, result);
                    AvaloniaLocator.Current.GetService<MainWindow>().Height = 300;
                    AvaloniaLocator.Current.GetService<MainWindow>().Width = 400;
                    AuthVM = authView;
                }
            }
        }

        private void OnIdentityChanged(object sender, IdentityChangedEventArgs e)
        {
            this.IdentityName = e.IdentityName;
            this.CurrentIdentity = _identityManager.CurrentIdentity;
        }

        public MainMenuViewModel()
        {
            this.Title = "SQRL Client";
        }

        public void OnNewIdentityClick()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new NewIdentityViewModel(this.sqrlInstance);
        }

        public void ExportIdentity()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new ExportIdentityViewModel(this.sqrlInstance);
        }

        public void ImportIdentity()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new ImportIdentityViewModel(this.sqrlInstance);
        }

        public async void SwitchIdentity()
        {
            if (_identityManager.IdentityCount > 1)
            {
                SelectIdentityDialogView selectIdentityDialog = new SelectIdentityDialogView();
                selectIdentityDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                await selectIdentityDialog.ShowDialog(AvaloniaLocator.Current.GetService<MainWindow>());
            }
        }

        public async void DeleteIdentity()
        {
            var msgBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                    $"Delete identity", 
                    $"Do you really want to delete the identity \"" + this.IdentityName + "\"?" + Environment.NewLine +
                    $"This cannot be undone!",
                    MessageBox.Avalonia.Enums.ButtonEnum.YesNo,
                    MessageBox.Avalonia.Enums.Icon.Warning);

            var result = await msgBox.ShowDialog(AvaloniaLocator.Current.GetService<MainWindow>());

            if (result == MessageBox.Avalonia.Enums.ButtonResult.Yes)
            {
                _identityManager.DeleteCurrentIdentity();
            }
        }

        public void IdentitySettings()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = 
                new IdentitySettingsViewModel(this.sqrlInstance);
        }

        public void Exit()
        {
            AvaloniaLocator.Current.GetService<MainWindow>().Close();
        }

        public void Login()
        {
            if(!string.IsNullOrEmpty(this.SiteUrl) && this.CurrentIdentity!=null)
            {
                if (Uri.TryCreate(this.SiteUrl, UriKind.Absolute, out Uri result))
                {
                    AuthenticationViewModel authView = new AuthenticationViewModel(this.sqrlInstance, this.CurrentIdentity, result);
                    AvaloniaLocator.Current.GetService<MainWindow>().Height = 300;
                    AvaloniaLocator.Current.GetService<MainWindow>().Width = 400;
                    AuthVM = authView;
                    ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = AuthVM;
                }
            }
        }
    }

}
