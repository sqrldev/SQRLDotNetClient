
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
        private bool _passwordsMatch = true;
        private string _newPassword = "";
        private string _newPasswordVerification = "";

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
            Init();
            this.Identity = identity;
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
            var progressBlock1 = new Progress<KeyValuePair<int, string>>();
            var progressBlock2 = new Progress<KeyValuePair<int, string>>();
            var progressDecryptBlock2 = new Progress<KeyValuePair<int, string>>();

            var progressDialog = new ProgressDialog(new List<Progress<KeyValuePair<int, string>>>() { 
                progressBlock1, progressBlock2, progressDecryptBlock2 });
            _ = progressDialog.ShowDialog(_mainWindow);

            var iukData = await SQRL.DecryptBlock2(
                this.Identity, SQRL.CleanUpRescueCode(this.RescueCode), progressDecryptBlock2);

            if (iukData.DecryptionSucceeded)
            {
                SQRLIdentity newId = this.Identity.Clone();
                byte[] imk = SQRL.CreateIMK(iukData.Iuk);

                if (!newId.HasBlock(0)) SQRL.GenerateIdentityBlock0(imk, newId);
                var block1 = SQRL.GenerateIdentityBlock1(iukData.Iuk, this.NewPassword, newId, progressBlock1);
                var block2 = SQRL.GenerateIdentityBlock2(iukData.Iuk, SQRL.CleanUpRescueCode(this.RescueCode), newId, progressBlock2);
                await Task.WhenAll(block1, block2);

                progressDialog.Close();

                if (newId.HasBlock(3)) SQRL.GenerateIdentityBlock3(iukData.Iuk, this.Identity, newId, imk, imk); 

                newId.IdentityName = this.IdentityName;

                try
                {
                    _identityManager.ImportIdentity(newId, true);
                }
                catch (InvalidOperationException e)
                {
                    var btnRsult = await new Views.MessageBox(
                        _loc.GetLocalizationValue("ErrorTitleGeneric"), e.Message,
                        MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                        .ShowDialog<MessagBoxDialogResult>(_mainWindow);
                }
                finally
                {
                    ((MainWindowViewModel)_mainWindow.DataContext).Content =
                    ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
                }
            }
            else
            {
                progressDialog.Close();

                var btnRsult = await new Views.MessageBox(
                    _loc.GetLocalizationValue("ErrorTitleGeneric"),
                    _loc.GetLocalizationValue("InvalidRescueCodeMessage"),
                    MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                    .ShowDialog<MessagBoxDialogResult>(_mainWindow);
            }
        }
    }
}
