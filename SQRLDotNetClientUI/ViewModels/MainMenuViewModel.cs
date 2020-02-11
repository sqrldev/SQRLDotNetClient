using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using SQRLDotNetClientUI.DBContext;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using SQRLDotNetClientUI.Models;

namespace SQRLDotNetClientUI.ViewModels
{
    public class MainMenuViewModel : ViewModelBase
    {
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
            var userData = GetUserData();
            if (userData != null && !string.IsNullOrEmpty(userData.LastLoadedIdentity) && File.Exists(userData.LastLoadedIdentity))
            {
                this.CurrentIdentity = SQRLIdentity.FromFile(userData.LastLoadedIdentity);
                this.CurrentIdentity.IdentityName = Path.GetFileNameWithoutExtension(userData.LastLoadedIdentity);
                this.CurrentIdentity.FileName = userData.LastLoadedIdentity;
                this.IdentityName = this.CurrentIdentity.IdentityName;
            }

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

        private UserData GetUserData()
        {
            UserData result = null;
            using (var db = new SQLiteDBContext())
            {
                result = db.UserData.FirstOrDefault();
                if (result == null)
                {
                    UserData ud = new UserData();
                    ud.LastLoadedIdentity = string.Empty;
                    db.UserData.Add(ud);
                    db.SaveChanges();
                    result = ud;
                }

            }
            return result;
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
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new ExportIdentityViewModel(this.sqrlInstance, this.CurrentIdentity);
        }

        public void ImportIdentity()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new ImportIdentityViewModel(this.sqrlInstance);
        }

        public async void SwitchIdentity()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            FileDialogFilter fdf = new FileDialogFilter();
            fdf.Name = "SQRL Identity";
            fdf.Extensions.Add("sqrl");
            ofd.Filters.Add(fdf);
            var file = await ofd.ShowAsync(AvaloniaLocator.Current.GetService<MainWindow>());
            if (file != null && file.Length > 0)
            {
                this.CurrentIdentity = SQRLIdentity.FromFile(file[0]);
                this.CurrentIdentity.IdentityName = Path.GetFileNameWithoutExtension(file[0]);
                this.IdentityName = this.CurrentIdentity.IdentityName;
                UserData result = null;
                using (var db = new SQLiteDBContext())
                {
                    result = db.UserData.FirstOrDefault();
                    if (result != null)
                    {
                        result.LastLoadedIdentity = file[0];
                        db.SaveChanges();
                    }
                }
            }
        }

        public void IdentitySettings()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = 
                new IdentitySettingsViewModel(this.sqrlInstance, this.CurrentIdentity);
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
