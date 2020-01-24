using Avalonia;
using ReactiveUI;
using Sodium;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public string SiteID { get { return $"{this.Site.Host}"; } set => this.RaiseAndSetIfChanged(ref _siteID, value); }

        private int _Block1Progress = 0;

        public int Block1Progress { get => _Block1Progress; set => this.RaiseAndSetIfChanged(ref _Block1Progress, value); }

        public int MaxProgress { get => 100; }

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

        public async void Login()
        {
            var progressBlock1 = new Progress<KeyValuePair<int, string>>(percent =>
            {
                this.Block1Progress = (int)percent.Key;
            });

            var result = await this.sqrlInstance.DecryptBlock1(this.Identity, this.Password, progressBlock1);
            if(result.Item1)
            {
                
                var siteKvp = sqrlInstance.CreateSiteKey(this.Site, this.AltID, result.Item2);
                Dictionary<byte[], KeyPair> priorKvps = null;
                if (this.Identity.Block3 != null && this.Identity.Block3.Edition > 0)
                {
                    byte[] decryptedBlock3 = this.sqrlInstance.DecryptBlock3(result.Item2, this.Identity, out bool allGood);
                    List<byte[]> oldIUKs = new List<byte[]>();
                    if (allGood)
                    {
                        int skip = 0;
                        int ct = 0;
                        while (skip < decryptedBlock3.Length)
                        {
                            oldIUKs.Add(decryptedBlock3.Skip(skip).Take(32).ToArray());
                            skip += 32;
                            ;
                            if (++ct >= 3)
                                break;
                        }

                        SQRL.ZeroFillByteArray(ref decryptedBlock3);
                        priorKvps = this.sqrlInstance.CreatePriorSiteKeys(oldIUKs, this.Site, AltID);
                        oldIUKs.Clear();
                    }
                }
            }
            else
            {
                string badPasswordErrorTitle = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService.GetLocalizationValue("BadPasswordErrorTitle");
                string badPasswordError = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService.GetLocalizationValue("BadPasswordError");
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow($"{badPasswordErrorTitle}", $"{badPasswordError}", MessageBox.Avalonia.Enums.ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Error);
                await messageBoxStandardWindow.ShowDialog(AvaloniaLocator.Current.GetService<MainWindow>());
            }

        }
    }
}
