using ReactiveUI;
using Sodium;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using Avalonia.Controls;
using SQRLDotNetClientUI.Models;
using Serilog;
using System.Reflection;

namespace SQRLDotNetClientUI.ViewModels
{
    public class AuthenticationViewModel : ViewModelBase
    {
        public enum LoginAction
        {
            Login,
            Disable,
            Remove
        };

        private bool _NewUpdateAvailable=false;
        public bool NewUpdateAvailable
        {
            get => _NewUpdateAvailable;
            set { this.RaiseAndSetIfChanged(ref _NewUpdateAvailable, value); }
        }


        private LoginAction action = LoginAction.Login;
        public LoginAction Action
        {
            get => action; 
            set { this.RaiseAndSetIfChanged(ref action, value); }
        }

        private SQRL _sqrlInstance = SQRL.GetInstance(true);
        private QuickPassManager _quickPassManager = QuickPassManager.Instance;

        public Uri Site { get; set; }
        public string AltID { get; set; } = "";

        public string _password = "";
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        public bool AuthAction { get; set; }

        public string _siteID = "";
        public string SiteID 
        { 
            get { return $"{this.Site.Host}"; } 
            set => this.RaiseAndSetIfChanged(ref _siteID, value); 
        }

        public string _passwordLabel = "";
        public string PasswordLabel
        {
            get => _passwordLabel;
            set => this.RaiseAndSetIfChanged(ref _passwordLabel, value);
        }

        public string _identityName = "";
        public string IdentityName
        {
            get => _identityName;
            set => this.RaiseAndSetIfChanged(ref _identityName, value);
        }

        public bool _showIdentitySelector;
        public bool ShowIdentitySelector
        {
            get => _showIdentitySelector;
            set => this.RaiseAndSetIfChanged(ref _showIdentitySelector, value);
        }

        public bool _advancedFunctionsVisible = false;
        public bool AdvancedFunctionsVisible
        {
            get => _advancedFunctionsVisible;
            set => this.RaiseAndSetIfChanged(ref _advancedFunctionsVisible, value);
        }

        public bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        private int _Block1Progress = 0;

        public int Block1Progress 
        { 
            get => _Block1Progress; 
            set => this.RaiseAndSetIfChanged(ref _Block1Progress, value); 
        }

        public int MaxProgress { get => 100; }

        public AuthenticationViewModel()
        {
            Init();
            this.Site = new Uri("https://google.com");
            this.SiteID = this.Site.Host;
        }

        public AuthenticationViewModel(Uri site)
        {
            Init();
            this.Site = site;
            this.SiteID = site.Host;
        }

        private void Init()
        {
            this.Title = _loc.GetLocalizationValue("AuthenticationWindowTitle");
            this.IdentityName = _identityManager.CurrentIdentity?.IdentityName;
            _identityManager.IdentityChanged += IdentityChanged;
            _identityManager.IdentityCountChanged += IdentityCountChanged;

            IdentityCountChanged(this, new IdentityCountChangedEventArgs(
                _identityManager.IdentityCount));

            // Observe changes to the password/quickpass field
            // and initiate automatic quickpass login if available
            this.WhenAnyValue(x => x.Password).Subscribe(x =>
            {
                if (!_quickPassManager.HasQuickPass())
                    return;

                int quickPassLength = _identityManager.CurrentIdentity.Block1.HintLength;

                Log.Debug("QuickPassLength len: {QuickPassLength}, CurrentLength: {CurrentLength}",
                    quickPassLength, x.Length);

                if (x.Length == quickPassLength)
                {
                    Log.Information("Initiating login using QuickPass");
                    Login(useQuickPass: true);
                }
            });

            CheckForQuickPass();

            CheckForUpdate();

        }

        private async void CheckForUpdate()
        {
            this.NewUpdateAvailable = await GitHubApi.GitHubHelper.CheckForUpdates(
                Assembly.GetExecutingAssembly().GetName().Version);
        }

        /// <summary>
        /// Check if QuickPass is available for the currently selected identity
        /// and change the UI labels accordingly.
        /// </summary>
        private void CheckForQuickPass()
        {
            if (!_quickPassManager.HasQuickPass())
                this.PasswordLabel = _loc.GetLocalizationValue("PasswordLabel");
            else
                this.PasswordLabel = _loc.GetLocalizationValue("QuickPassLabel");
        }

        /// <summary>
        /// Dynamically react to identity additions/removals.
        /// </summary>
        private void IdentityCountChanged(object sender, IdentityCountChangedEventArgs e)
        {
            if (e.IdentityCount > 1) this.ShowIdentitySelector = true;
            else this.ShowIdentitySelector = false;
        }

        /// <summary>
        /// Opens the identity selection dialog.
        /// </summary>
        public void SwitchIdentity()
        {
            new SelectIdentityViewModel().ShowDialog(this);
        }

        /// <summary>
        /// React to identity changes.
        /// </summary>
        private void IdentityChanged(object sender, IdentityChangedEventArgs e)
        {
            this.IdentityName = e.Identity.IdentityName;
        }

        /// <summary>
        /// Shows advanced functions like disable/remove account etc.
        /// </summary>
        public void ShowAdvancedFunctions()
        {
            this.AdvancedFunctionsVisible = true;
        }

        public void Cancel()
        {
            if (_sqrlInstance.cps.PendingResponse)
            {
                _sqrlInstance.cps.cpsBC.Add(_sqrlInstance.cps.Can);
            }
            while (_sqrlInstance.cps.PendingResponse)
                ;
            ShowMainScreenAndClose();
        }

        public async void Login(bool useQuickPass = false)
        {
            byte[] imk, ilk;
            this.IsBusy = true;

            var progressBlock1 = new Progress<KeyValuePair<int, string>>(percent =>
            {
                this.Block1Progress = (int)percent.Key;
            });

            if (useQuickPass)
            {
                var keys = await _quickPassManager.GetQuickPassDecryptedImk(this.Password, null, progressBlock1);
                imk = keys.Imk;
                ilk = keys.Ilk;
            }
            else
            {
                var block1Keys = await SQRL.DecryptBlock1(_identityManager.CurrentIdentity, this.Password, progressBlock1);
                if (!block1Keys.DecryptionSucceeded)
                {
                    var dialogResult = await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("BadPasswordErrorTitle"), 
                                               _loc.GetLocalizationValue("BadPasswordError"), 
                                               MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                                               .ShowDialog(this);
                    if (dialogResult ==MessagBoxDialogResult.OK)
                    {
                        this.IsBusy = false;
                        return;
                    }
                }
                imk = block1Keys.Imk;
                ilk = block1Keys.Ilk;
            }

            // Block 1 was sucessfully decrypted using the master pasword,
            // so enable QuickPass if it isn't already set
            if (!_quickPassManager.HasQuickPass(_identityManager.CurrentIdentityUniqueId))
                _quickPassManager.SetQuickPass(this.Password, imk, ilk, _identityManager.CurrentIdentity);

            var siteKvp = SQRL.CreateSiteKey(this.Site, this.AltID, imk);

            var priorSiteKeys = GeneratePriorKeyInfo(imk);
            SQRLOptions sqrlOpts = new SQRLOptions(SQRLOptions.SQRLOpts.CPS | SQRLOptions.SQRLOpts.SUK);
            var serverResponse = SQRL.GenerateQueryCommand(this.Site, siteKvp, sqrlOpts, null, 0, priorSiteKeys);

            if (serverResponse.CommandFailed)
            {
                //TODO: Fix Dialog
                var dialogResult = await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                                           _loc.GetLocalizationValue("SQRLCommandFailedUnknown"), 
                                           MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                                           .ShowDialog(this);
                if (dialogResult == MessagBoxDialogResult.OK)
                {
                    this.IsBusy = false;
                    return;
                }
            }

            // New account, ask if they want to create one
            if (!serverResponse.CurrentIDMatch && !serverResponse.PreviousIDMatch)
            {
                serverResponse = await HandleNewAccount(imk, ilk, siteKvp, sqrlOpts, serverResponse);
            }
            // A previous id matches, replace the outdated id on the server with the latest
            else if (serverResponse.PreviousIDMatch)
            {
                byte[] ursKey = null;
                ursKey = SQRL.GetURSKey(serverResponse.PriorMatchedKey.Key, Utilities.Base64ToBinary(serverResponse.SUK, string.Empty, Utilities.Base64Variant.UrlSafeNoPadding));
                StringBuilder additionalData = null;
                if (!string.IsNullOrEmpty(serverResponse.SIN))
                {
                    additionalData = new StringBuilder();
                    byte[] ids = SQRL.CreateIndexedSecret(this.Site, AltID, imk, Encoding.UTF8.GetBytes(serverResponse.SIN));
                    additionalData.AppendLineWindows($"ins={Utilities.BinaryToBase64(ids, Utilities.Base64Variant.UrlSafeNoPadding)}");
                    byte[] pids = SQRL.CreateIndexedSecret(serverResponse.PriorMatchedKey.Value.SiteSeed, Encoding.UTF8.GetBytes(serverResponse.SIN));
                    additionalData.AppendLineWindows($"pins={Utilities.BinaryToBase64(pids, Utilities.Base64Variant.UrlSafeNoPadding)}");

                }
                serverResponse = SQRL.GenerateIdentCommandWithReplace(serverResponse.NewNutURL, siteKvp, serverResponse.FullServerRequest, 
                    ilk, ursKey, serverResponse.PriorMatchedKey.Value.KeyPair, sqrlOpts, additionalData);
            }
            // Current id matches 
            else if (serverResponse.CurrentIDMatch)
            {
                int askResponse = 0;
                if (serverResponse.HasAsk)
                {
                    MainWindow w = new MainWindow();

                    var mwTemp = new MainWindowViewModel();
                    w.DataContext = mwTemp;
                    w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    var avm = new AskViewModel(serverResponse)
                    {
                        CurrentWindow = w
                    };
                    mwTemp.Content = avm;
                    askResponse = await w.ShowDialog<int>(_mainWindow);
                }

                StringBuilder addClientData = null;
                if (askResponse > 0)
                {
                    addClientData = new StringBuilder();
                    addClientData.AppendLineWindows($"btn={askResponse}");
                }
                if (serverResponse.SQRLDisabled)
                {
                    var disabledAccountAlert = string.Format(_loc.GetLocalizationValue("SqrlDisabledAlert"), this.SiteID, Environment.NewLine);
                    
                    var btResult =await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("ReEnableSQRLTitle").ToUpper(),
                                                             disabledAccountAlert,
                                                             MessageBoxSize.Medium, MessageBoxButtons.YesNo, MessageBoxIcons.QUESTION)
                                                            .ShowDialog(this);
                    if (btResult == MessagBoxDialogResult.YES)
                    {
                        RetryRescueCode:
                        InputSecretDialogViewModel rescueCodeDlg = new InputSecretDialogViewModel(SecretType.RescueCode);
                        var dialogClosed = await rescueCodeDlg.ShowDialog(this);

                        if (dialogClosed)
                        {
                            var iukData = await SQRL.DecryptBlock2(_identityManager.CurrentIdentity, SQRL.CleanUpRescueCode(rescueCodeDlg.Secret), progressBlock1);
                            if (iukData.DecryptionSucceeded)
                            {
                                byte[] ursKey = null;
                                ursKey = SQRL.GetURSKey(iukData.Iuk, Utilities.Base64ToBinary(serverResponse.SUK, string.Empty, Utilities.Base64Variant.UrlSafeNoPadding));

                                iukData.Iuk.ZeroFill();
                                serverResponse = SQRL.GenerateEnableCommand(serverResponse.NewNutURL, siteKvp, serverResponse.FullServerRequest, ursKey, addClientData, sqrlOpts);
                            }
                            else
                            {

                                var answer = await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                                                                        _loc.GetLocalizationValue("InvalidRescueCodeMessage"),
                                                                        MessageBoxSize.Small, MessageBoxButtons.YesNo, MessageBoxIcons.ERROR)
                                                                        .ShowDialog(this);
                                if (answer == MessagBoxDialogResult.YES)
                                {
                                    goto RetryRescueCode;
                                }
                            }
                        }
                    }

                }
                //Here
                switch (Action)
                {
                    case LoginAction.Login:
                        {
                            addClientData = GenerateSIN(imk, serverResponse, addClientData);
                            serverResponse = SQRL.GenerateSQRLCommand(SQRLCommands.ident, serverResponse.NewNutURL, siteKvp, serverResponse.FullServerRequest, addClientData, sqrlOpts);
                            if (SQRL.GetInstance(true).cps != null && _sqrlInstance.cps.PendingResponse)
                            {
                                _sqrlInstance.cps.cpsBC.Add(new Uri(serverResponse.SuccessUrl));
                            }
                            while (_sqrlInstance.cps.PendingResponse)
                                ;
                            ShowMainScreenAndClose();
                        }
                        break;
                    case LoginAction.Disable:
                        {
                            var disableAccountAlert = string.Format(_loc.GetLocalizationValue("DisableAccountAlert"), this.SiteID, Environment.NewLine);
                            
                            var btResult = await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("WarningMessageBoxTitle").ToUpper(),
                                                                     disableAccountAlert, 
                                                                     MessageBoxSize.Large, MessageBoxButtons.YesNo, MessageBoxIcons.QUESTION)
                                                                    .ShowDialog(this);
                            if (btResult == MessagBoxDialogResult.YES)
                            {
                                GenerateSIN(imk, serverResponse, addClientData);
                                serverResponse = SQRL.GenerateSQRLCommand(SQRLCommands.disable, serverResponse.NewNutURL, siteKvp, serverResponse.FullServerRequest, addClientData, sqrlOpts);
                                if (_sqrlInstance.cps != null && _sqrlInstance.cps.PendingResponse)
                                {
                                    _sqrlInstance.cps.cpsBC.Add(_sqrlInstance.cps.Can);
                                }
                                while (_sqrlInstance.cps.PendingResponse)
                                    ;
                                ShowMainScreenAndClose();
                            }
                        }
                        break;
                    case LoginAction.Remove:
                        {
                            InputSecretDialogViewModel rescueCodeDlg = new InputSecretDialogViewModel(SecretType.RescueCode);
                            
                            var dialogClosed = await rescueCodeDlg.ShowDialog(this);
                            if (dialogClosed)
                            {
                                var iukData = await SQRL.DecryptBlock2(_identityManager.CurrentIdentity, SQRL.CleanUpRescueCode(rescueCodeDlg.Secret), progressBlock1);
                                if (iukData.DecryptionSucceeded)
                                {
                                    byte[] ursKey = SQRL.GetURSKey(iukData.Iuk, Sodium.Utilities.Base64ToBinary(serverResponse.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));

                                    serverResponse = SQRL.GenerateSQRLCommand(SQRLCommands.remove, serverResponse.NewNutURL, siteKvp, serverResponse.FullServerRequest, addClientData, sqrlOpts, null, ursKey);
                                    if (_sqrlInstance.cps != null && _sqrlInstance.cps.PendingResponse)
                                    {
                                        _sqrlInstance.cps.cpsBC.Add(_sqrlInstance.cps.Can);
                                    }
                                    while (_sqrlInstance.cps.PendingResponse)
                                        ;
                                    ShowMainScreenAndClose();
                                }
                                else
                                {


                                    _ = await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                                                               _loc.GetLocalizationValue("InvalidRescueCodeMessage"),
                                                               MessageBoxSize.Small, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                                                               .ShowDialog(this);
                                }
                                
                            }
                        }
                        break;
                }
            }

            this.IsBusy = false;
        }

        private StringBuilder GenerateSIN(byte[] imk, SQRLServerResponse serverResponse, StringBuilder addClientData)
        {
            if (!string.IsNullOrEmpty(serverResponse.SIN))
            {
                if (addClientData == null)
                    addClientData = new StringBuilder();
                byte[] ids = SQRL.CreateIndexedSecret(this.Site, AltID, imk, Encoding.UTF8.GetBytes(serverResponse.SIN));
                addClientData.AppendLineWindows($"ins={Sodium.Utilities.BinaryToBase64(ids, Utilities.Base64Variant.UrlSafeNoPadding)}");
            }

            return addClientData;
        }

        private async System.Threading.Tasks.Task<SQRLServerResponse> HandleNewAccount(byte[] imk, byte[] ilk, KeyPair siteKvp, SQRLOptions sqrlOpts, SQRLServerResponse serverResponse)
        {
            string newAccountQuestion = string.Format(_loc.GetLocalizationValue("NewAccountQuestion"), this.SiteID);
            string genericQuestionTitle = string.Format(_loc.GetLocalizationValue("GenericQuestionTitle"), this.SiteID);

            
            var btnRsult = await new Views.MessageBoxViewModel(genericQuestionTitle,
                                                      newAccountQuestion, 
                                                      MessageBoxSize.Medium, MessageBoxButtons.YesNo, MessageBoxIcons.QUESTION)
                                                      .ShowDialog(this);
            if (btnRsult == MessagBoxDialogResult.YES)
            {
                StringBuilder additionalData = null;
                if (!string.IsNullOrEmpty(serverResponse.SIN))
                {
                    additionalData = new StringBuilder();
                    byte[] ids = SQRL.CreateIndexedSecret(this.Site, AltID, imk, Encoding.UTF8.GetBytes(serverResponse.SIN));
                    additionalData.AppendLineWindows($"ins={Sodium.Utilities.BinaryToBase64(ids, Utilities.Base64Variant.UrlSafeNoPadding)}");
                }
                serverResponse = SQRL.GenerateNewIdentCommand(serverResponse.NewNutURL, siteKvp, serverResponse.FullServerRequest, ilk, sqrlOpts, additionalData);
                if (!serverResponse.CommandFailed)
                {
                    if (_sqrlInstance.cps.PendingResponse)
                    {
                        _sqrlInstance.cps.cpsBC.Add(new Uri(serverResponse.SuccessUrl));
                    }
                    while (_sqrlInstance.cps.PendingResponse)
                        ;
                    ShowMainScreenAndClose();
                }
            }
            else
            {
                if (_sqrlInstance.cps.PendingResponse)
                {
                    _sqrlInstance.cps.cpsBC.Add(_sqrlInstance.cps.Can);
                }
                while (_sqrlInstance.cps.PendingResponse)
                    ;
                ShowMainScreenAndClose();
            }

            return serverResponse;
        }

        private Dictionary<byte[], PriorSiteKeysResult> GeneratePriorKeyInfo(byte[] imk)
        {
            Dictionary<byte[], PriorSiteKeysResult> priorSiteKeys = null;

            if (_identityManager.CurrentIdentity.Block3 != null && 
                _identityManager.CurrentIdentity.Block3.Edition > 0)
            {
                byte[] decryptedBlock3 = SQRL.DecryptBlock3(imk, _identityManager.CurrentIdentity, out bool allGood);
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
                    priorSiteKeys = SQRL.CreatePriorSiteKeys(oldIUKs, this.Site, AltID);
                    foreach (var piuk in oldIUKs) piuk.ZeroFill();
                    oldIUKs.Clear();
                }
            }

            return priorSiteKeys;
        }

        /// <summary>
        /// Sets the content of the window to the main page and closes the window.
        /// </summary>
        private void ShowMainScreenAndClose()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
            _mainWindow.Close();
        }
    }
}
