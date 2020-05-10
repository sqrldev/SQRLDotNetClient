
using ReactiveUI;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.ViewModels
{ 
    /// <summary>
    /// A viewmodel providing application logic for <c>ImportIdentitySetupView</c>.
    /// </summary>
    public class ImportIdentitySetupViewModel : ViewModelBase
    {
        private bool _canSave = true;
        private bool _importWithPassword = true;
        private bool _passwordsMatch = true;
        private string _password = "";
        private string _newPassword = "";
        private string _newPasswordVerification = "";
        private string _importSetupMessage = "";

        /// <summary>
        /// The SQRL identity to be imported.
        /// </summary>
        public SQRLIdentity Identity { get; set; }

        /// <summary>
        /// The identity name entered by the user.
        /// </summary>
        public string IdentityName { get; set; } = "";

        /// <summary>
        /// The rescue code entered by the user.
        /// </summary>
        public string RescueCode { get; set; }

        /// <summary>
        /// Gets or sets if its possible to hit the "OK" button on 
        /// the dialog to actually import the identity.
        /// </summary>
        public bool CanSave
        {
            get => _canSave;
            set => this.RaiseAndSetIfChanged(ref _canSave, value);
        }

        /// <summary>
        /// Gets or sets if the identity to be imported has a block
        /// of type 1 and can therefore be imported using the password,
        /// or if the rescue code is required.
        /// </summary>
        public bool ImportWithPassword
        {
            get => _importWithPassword;
            set
            {
                this.RaiseAndSetIfChanged(ref _importWithPassword, value);
                this.ImportSetupMessage = value ?
                    _loc.GetLocalizationValue("ImportSetupMessagePassword") :
                    _loc.GetLocalizationValue("ImportSetupMessageRescueCode");
            }
        }

        /// <summary>
        /// Gets or sets if the new password and the password 
        /// verification are equal.
        /// </summary>
        public bool PasswordsMatch
        {
            get => _passwordsMatch;
            set
            {
                this.RaiseAndSetIfChanged(ref _passwordsMatch, value);
                this.CanSave = value;
            }
        }

        /// <summary>
        /// The master password of the identity to be imported.
        /// </summary>
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        /// <summary>
        /// The new password entered by the user.
        /// </summary>
        public string NewPassword
        {
            get => _newPassword;
            set => this.RaiseAndSetIfChanged(ref _newPassword, value);
        }

        /// <summary>
        /// The verification of the new password entered by the user.
        /// </summary>
        public string NewPasswordVerification
        {
            get => _newPasswordVerification;
            set => this.RaiseAndSetIfChanged(ref _newPasswordVerification, value);
        }

        /// <summary>
        /// The message to display at the top of the import setup screen.
        /// </summary>
        public string ImportSetupMessage
        {
            get => _importSetupMessage;
            set => this.RaiseAndSetIfChanged(ref _importSetupMessage, value);
        }

        /// <summary>
        /// Creates a new <c>ImportIdentitySetupViewModel</c> instance.
        /// </summary>
        public ImportIdentitySetupViewModel()
        {
            Init();
        }

        /// <summary>
        /// Creates a new <c>ImportIdentitySetupViewModel</c> instance
        /// and sets the identity to be imorted.
        /// </summary>
        /// <param name="identity">The identity to be imported.</param>
        public ImportIdentitySetupViewModel(SQRLIdentity identity)
        {
            this.Identity = identity;
            this.ImportWithPassword = this.Identity.HasBlock(1);
            Init();
        }

        /// <summary>
        /// Used by the c-tors to initialize stuff.
        /// </summary>
        private void Init()
        {
            this.Title = _loc.GetLocalizationValue("ImportIdentitySetupWindowTitle");
        }

        /// <summary>
        /// Takes the user back to the previous screen.
        /// </summary>
        public void Previous()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
        }

        /// <summary>
        /// Takes the user back to the main screen.
        /// </summary>
        public void Cancel()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
        }

        /// <summary>
        /// Verifies and actually imports the identity.
        /// </summary>
        public async void VerifyAndImportIdentity()
        {
            var prgEncBlock1 = new Progress<KeyValuePair<int, string>>();
            var prgEncBlock2 = new Progress<KeyValuePair<int, string>>();
            var prgDecBlock1 = new Progress<KeyValuePair<int, string>>();
            var prgDecBlock2 = new Progress<KeyValuePair<int, string>>();

            List<Progress<KeyValuePair<int, string>>> progressList = this.ImportWithPassword ?
                new List<Progress<KeyValuePair<int, string>>>() { prgEncBlock1, prgDecBlock1 } :
                new List<Progress<KeyValuePair<int, string>>>() { prgEncBlock1, prgEncBlock2, prgDecBlock2 };

            var progressDialog = new ProgressDialogViewModel(progressList, this);
            progressDialog.ShowDialog();

            SQRLIdentity newId = this.Identity.Clone();

            if (this.ImportWithPassword)
            {
                var block1Keys = await SQRL.DecryptBlock1(this.Identity, this.Password, prgDecBlock1);

                if (!block1Keys.DecryptionSucceeded)
                {
                    progressDialog.Close();

                    var btnRsult = await new MessageBoxViewModel(
                        _loc.GetLocalizationValue("ErrorTitleGeneric"),
                        _loc.GetLocalizationValue("InvalidPasswordMessage"),
                        MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                        .ShowDialog(this);

                    return;
                }

                if (!newId.HasBlock(0)) SQRL.GenerateIdentityBlock0(block1Keys.Imk, newId);

                newId = await SQRL.GenerateIdentityBlock1(block1Keys.Imk, block1Keys.Ilk, 
                    this.Password, newId, prgEncBlock1);

                if (newId.HasBlock(3)) SQRL.GenerateIdentityBlock3(
                    null, this.Identity, newId, block1Keys.Imk, block1Keys.Imk);
            }
            else
            {
                var iukData = await SQRL.DecryptBlock2(this.Identity, 
                    SQRL.CleanUpRescueCode(this.RescueCode), prgDecBlock2);

                if (!iukData.DecryptionSucceeded)
                {
                    progressDialog.Close();

                    var btnRsult = await new MessageBoxViewModel(
                        _loc.GetLocalizationValue("ErrorTitleGeneric"),
                        _loc.GetLocalizationValue("InvalidRescueCodeMessage"),
                        MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                        .ShowDialog(this);

                    return;
                }

                byte[] imk = SQRL.CreateIMK(iukData.Iuk);

                if (!newId.HasBlock(0)) SQRL.GenerateIdentityBlock0(imk, newId);
                var block1 = SQRL.GenerateIdentityBlock1(iukData.Iuk, this.NewPassword, newId, prgEncBlock1);
                var block2 = SQRL.GenerateIdentityBlock2(iukData.Iuk, SQRL.CleanUpRescueCode(this.RescueCode), newId, prgEncBlock2);
                await Task.WhenAll(block1, block2);

                if (newId.HasBlock(3)) SQRL.GenerateIdentityBlock3(iukData.Iuk, this.Identity, newId, imk, imk);
            }

            progressDialog.Close();
            newId.IdentityName = this.IdentityName;

            try
            {
                _identityManager.ImportIdentity(newId, true);
            }
            catch (InvalidOperationException e)
            {
                var btnRsult = await new MessageBoxViewModel(
                    _loc.GetLocalizationValue("ErrorTitleGeneric"), e.Message,
                    MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                    .ShowDialog(this);
            }
            finally
            {
                ((MainWindowViewModel)_mainWindow.DataContext).Content =
                    ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
            }
        }
    }
}
