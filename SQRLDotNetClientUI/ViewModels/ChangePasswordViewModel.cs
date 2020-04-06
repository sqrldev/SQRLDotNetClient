using ReactiveUI;
using Serilog;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// A view model providing application logic for the 
    /// <c>ChangePasswordView</c> screeen.
    /// </summary>
    class ChangePasswordViewModel : ViewModelBase
    {
        private bool _canSave = true;
        private bool _passwordsMatch = true;
        private string _password = "";
        private string _newPassword = "";
        private string _newPasswordVerification = "";

        /// <summary>
        /// Gets or sets if its possible to hit the "OK" button on 
        /// the dialog to save the newly set password.
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
        /// The current password entered by the user.
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
        /// Creates a new <c>ChangePasswordViewModel</c> instance and sets the
        /// dialog's window title.
        /// </summary>
        public ChangePasswordViewModel()
        {
            this.Title = _loc.GetLocalizationValue("ChangePasswordDialogTitle");
            if (_identityManager.CurrentIdentity != null) 
                this.Title += " (" + _identityManager.CurrentIdentity.IdentityName + ")";
        }

        /// <summary>
        /// Closes the current view and displays the main screen.
        /// </summary>
        public void Close()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
        }

        /// <summary>
        /// Decrypts the current identity's block 1 using the old password
        /// and tries to re-encrypt it using the new password.
        /// </summary>
        public async void SetNewPassword()
        {
            CanSave = false;

            var progress = new Progress<KeyValuePair<int, string>>();
            
            var progressDialog = new ProgressDialogViewModel(progress,this,false);
            progressDialog.ShowDialog();

            var block1Keys = await SQRL.DecryptBlock1(_identityManager.CurrentIdentity, 
                this.Password, progress);

            if (!block1Keys.DecryptionSucceeded)
            {
                
                progressDialog.Close();
                Log.Information("Bad password was supplied for identity id {IdentityUniqueId}",
                _identityManager.CurrentIdentityUniqueId);

                _ = await new MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                    _loc.GetLocalizationValue("BadPasswordError"),
                    MessageBoxSize.Small, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                    .ShowDialog(this);

                CanSave = true;
                return;
            }

            // Decryption succeeded, let's go ahead
            var currentId = _identityManager.CurrentIdentity;
            var idCopy = _identityManager.CurrentIdentity.Clone();
            await SQRL.GenerateIdentityBlock1(block1Keys.Imk, block1Keys.Ilk, this.NewPassword, currentId, progress, (int)currentId.Block1.PwdVerifySeconds);
            
            progressDialog.Close();

            // Write the changes back to the db
            _identityManager.UpdateCurrentIdentity();

            Log.Information("Password was changed for identity id {IdentityUniqueId}",
                _identityManager.CurrentIdentityUniqueId);

            // And finally clear the QuickPass for the current identity
            QuickPassManager.Instance.ClearQuickPass(
                _identityManager.CurrentIdentityUniqueId, QuickPassClearReason.PasswordChange);

            CanSave = true;
            Close();
        }
    }
}
