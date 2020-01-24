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
        public SQRL sqrlInstance { get; set; }
        public SQRLIdentity currentIdentity { get; set; }

        public String _IdentityName = "";
        public String IdentityName { get => _IdentityName; set => this.RaiseAndSetIfChanged(ref _IdentityName, value); }

        public AuthenticationViewModel AuthVM { get; set; }
   
        public MainMenuViewModel(SQRL sqrlInstance)
        {
            this.Title = "SQRL Client";
            this.sqrlInstance = sqrlInstance;
            var userData = GetUserData();
            if (userData != null && !string.IsNullOrEmpty(userData.LastLoadedIdentity))
            {
                this.currentIdentity = SQRLIdentity.FromFile(userData.LastLoadedIdentity);
                this.currentIdentity.IdentityName = Path.GetFileNameWithoutExtension(userData.LastLoadedIdentity);
                this.IdentityName = this.currentIdentity.IdentityName;
            }

            string[] commandLine = Environment.CommandLine.Split(" ");
            if(commandLine.Length>1)
            {

               if (Uri.TryCreate(commandLine[0], UriKind.Absolute, out Uri result))
                {
                    AuthenticationViewModel authView = new AuthenticationViewModel(this.sqrlInstance, this.currentIdentity, result);
                    AvaloniaLocator.Current.GetService<MainWindow>().Height = 200;
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
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new ExportIdentityViewModel(this.sqrlInstance, this.currentIdentity);
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
                this.currentIdentity = SQRLIdentity.FromFile(file[0]);
                this.currentIdentity.IdentityName = Path.GetFileNameWithoutExtension(file[0]);
                this.IdentityName = this.currentIdentity.IdentityName;
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
    }

}
