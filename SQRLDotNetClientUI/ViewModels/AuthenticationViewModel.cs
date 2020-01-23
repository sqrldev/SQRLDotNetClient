using Avalonia;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    public class AuthenticationViewModel: ViewModelBase
    {
        public Uri Site { get; set; }
        public SQRL sqrlInstance { get; set; }
        public SQRLIdentity Identity { get; set; }
        public string AltID { get; set; }
        public string Password { get; set; }

        public string _siteID = "";
        public string SiteID { get { return $"Authenticate To: {this.Site.Host}"; } set => this.RaiseAndSetIfChanged(ref _siteID, value); }
    

        public AuthenticationViewModel()
        {
            this.Title = "SQRL Client - Authentication";
            this.Site = new Uri("https://google.com");
        }

        public AuthenticationViewModel(SQRL sqrlInstance, SQRLIdentity identity, Uri site)
        {
            this.sqrlInstance = sqrlInstance;
            this.Identity = identity;
            this.Site = site;
            this.SiteID = site.Host;
        }

        public void Cancel()
        {
            if(this.sqrlInstance.cps.PendingResponse)
            {
                this.sqrlInstance.cps.cpsBC.Add(this.sqrlInstance.cps.Can);
            }
            AvaloniaLocator.Current.GetService<MainWindow>().Close();
        }

        public void Login()
        {

        }
    }
}
