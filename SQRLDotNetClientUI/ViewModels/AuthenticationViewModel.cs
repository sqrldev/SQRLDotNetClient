using Avalonia;
using ReactiveUI;
using Sodium;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using MessageBox.Avalonia.Views;
using SQRLDotNetClientUI.Utils;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;

namespace SQRLDotNetClientUI.ViewModels
{
    public class AuthenticationViewModel: ViewModelBase
    {
        public Uri Site { get; set; }
        public SQRL sqrlInstance { get; set; }
        public SQRLIdentity Identity { get; set; }
        public string AltID { get; set; }
        public string Password { get; set; }

        public bool AuthAction { get; set; }

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
            while (this.sqrlInstance.cps.PendingResponse)
                ;
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

                Dictionary<byte[], Tuple<byte[], KeyPair>> priorKvps = null;
                priorKvps = GeneratePriorKeyInfo(result, priorKvps);
                SQRLOptions sqrlOpts = new SQRLOptions(SQRLOptions.SQRLOpts.CPS | SQRLOptions.SQRLOpts.SUK);
                var serverResponse = this.sqrlInstance.GenerateQueryCommand(this.Site, siteKvp, sqrlOpts, null, 0, priorKvps);
                if(!serverResponse.CommandFailed)
                {
                    // New Account ask if they want to create one

                    if (!serverResponse.CurrentIDMatch && !serverResponse.PreviousIDMatch)
                    {
                        serverResponse = await HandleNewAccount(result, siteKvp, sqrlOpts, serverResponse);
                    }
                    else if(serverResponse.PreviousIDMatch)
                    {
                        byte[] ursKey = null;
                        ursKey = this.sqrlInstance.GetURSKey(serverResponse.PriorMatchedKey.Key, Sodium.Utilities.Base64ToBinary(serverResponse.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
                        StringBuilder additionalData = null;
                        if (!string.IsNullOrEmpty(serverResponse.SIN))
                        {
                            additionalData = new StringBuilder();
                            byte[] ids = this.sqrlInstance.CreateIndexedSecret(this.Site, AltID, result.Item2, Encoding.UTF8.GetBytes(serverResponse.SIN));
                            additionalData.AppendLineWindows($"ins={Sodium.Utilities.BinaryToBase64(ids, Utilities.Base64Variant.UrlSafeNoPadding)}");
                            byte[] pids = this.sqrlInstance.CreateIndexedSecret(serverResponse.PriorMatchedKey.Value.Item1, Encoding.UTF8.GetBytes(serverResponse.SIN));
                            additionalData.AppendLineWindows($"pins={Sodium.Utilities.BinaryToBase64(pids, Utilities.Base64Variant.UrlSafeNoPadding)}");

                        }
                        serverResponse = this.sqrlInstance.GenerateIdentCommandWithReplace(serverResponse.NewNutURL, siteKvp, serverResponse.FullServerRequest, result.Item3, ursKey, serverResponse.PriorMatchedKey.Value.Item2, sqrlOpts, additionalData);
                    }
                    else if(serverResponse.CurrentIDMatch)
                    {
                        int askResponse = 0;
                        if (serverResponse.HasAsk)
                        {
                            MainWindow w = new MainWindow();

                            var mwTemp = new MainWindowViewModel(this.sqrlInstance);
                            w.DataContext = mwTemp;
                            w.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner;
                            var avm = new AskViewModel(this.sqrlInstance, this.Identity, serverResponse)
                            {
                                CurrentWindow = w
                            };
                            mwTemp.Content = avm;
                            askResponse = await w.ShowDialog<int>(AvaloniaLocator.Current.GetService<MainWindow>());
                        }

                        StringBuilder addClientData = null;
                        if (askResponse > 0)
                        {
                            addClientData = new StringBuilder();
                            addClientData.AppendLineWindows($"btn={askResponse}");
                        }
                    }
                }
                else
                {
                   var errorTitle= AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService.GetLocalizationValue("ErrorTitleGeneric");
                   var genericSqrlError = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService.GetLocalizationValue("SQRLCommandFailedUnknown");
                   var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow($"{errorTitle}", $"{genericSqrlError}", MessageBox.Avalonia.Enums.ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Error);
                   await messageBoxStandardWindow.ShowDialog(AvaloniaLocator.Current.GetService<MainWindow>());
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

        private async System.Threading.Tasks.Task<SQRLServerResponse> HandleNewAccount(Tuple<bool, byte[], byte[]> result, KeyPair siteKvp, SQRLOptions sqrlOpts, SQRLServerResponse serverResponse)
        {
            string newAccountQuestion = string.Format(AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService.GetLocalizationValue("NewAccountQuestion"), this.SiteID);
            string genericQuestionTitle = string.Format(AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService.GetLocalizationValue("GenericQuestionTitle"), this.SiteID);

            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow($"{genericQuestionTitle}", $"{newAccountQuestion}", MessageBox.Avalonia.Enums.ButtonEnum.YesNo, MessageBox.Avalonia.Enums.Icon.Plus);

            messageBoxStandardWindow.SetMessageStartupLocation(Avalonia.Controls.WindowStartupLocation.CenterOwner);


            var btnRsult = await messageBoxStandardWindow.ShowDialog(AvaloniaLocator.Current.GetService<MainWindow>());
            if (btnRsult == MessageBox.Avalonia.Enums.ButtonResult.Yes)
            {
                StringBuilder additionalData = null;
                if (!string.IsNullOrEmpty(serverResponse.SIN))
                {
                    additionalData = new StringBuilder();
                    byte[] ids = this.sqrlInstance.CreateIndexedSecret(this.Site, AltID, result.Item2, Encoding.UTF8.GetBytes(serverResponse.SIN));
                    additionalData.AppendLineWindows($"ins={Sodium.Utilities.BinaryToBase64(ids, Utilities.Base64Variant.UrlSafeNoPadding)}");
                }
                serverResponse = this.sqrlInstance.GenerateNewIdentCommand(serverResponse.NewNutURL, siteKvp, serverResponse.FullServerRequest, result.Item3, sqrlOpts, additionalData);
                if (!serverResponse.CommandFailed)
                {
                    if (this.sqrlInstance.cps.PendingResponse)
                    {
                        this.sqrlInstance.cps.cpsBC.Add(new Uri(serverResponse.SuccessUrl));
                    }
                    while (this.sqrlInstance.cps.PendingResponse)
                        ;
                    AvaloniaLocator.Current.GetService<MainWindow>().Close();
                }
            }
            else
            {
                if (this.sqrlInstance.cps.PendingResponse)
                {
                    this.sqrlInstance.cps.cpsBC.Add(this.sqrlInstance.cps.Can);
                }
                while (this.sqrlInstance.cps.PendingResponse)
                    ;
                AvaloniaLocator.Current.GetService<MainWindow>().Close();
            }

            return serverResponse;
        }

        private Dictionary<byte[], Tuple<byte[], KeyPair>> GeneratePriorKeyInfo(Tuple<bool, byte[], byte[]> result, Dictionary<byte[], Tuple<byte[], KeyPair>> priorKvps)
        {
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

            return priorKvps;
        }
    }
}
