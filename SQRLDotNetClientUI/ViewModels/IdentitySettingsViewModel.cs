using Avalonia;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// A view model representing the app's "Identity Settings" screen.
    /// </summary>
    class IdentitySettingsViewModel : ViewModelBase
    {
        private bool _canSave = true;

        /// <summary>
        /// Gets or sets the affected identity.
        /// </summary>
        public SQRLIdentity Identity { get; set; }

        /// <summary>
        /// Gets or sets a working copy of the affected identity for
        /// holding unsaved changes.
        /// </summary>
        public SQRLIdentity IdentityCopy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether changes were made to
        /// the affected identity which can be saved.
        /// </summary>
        public bool CanSave 
        { 
            get => _canSave; 
            set => this.RaiseAndSetIfChanged(ref _canSave, value); 
        }

        /// <summary>
        /// Creates a new <c>IdentitySettingsViewModel</c> instance and performs
        /// some initialization tasks.
        /// </summary>
        public IdentitySettingsViewModel()
        {
            this.Title = _loc.GetLocalizationValue("IdentitySettingsDialogTitle");
            this.Identity = _identityManager.CurrentIdentity;
            this.IdentityCopy = this.Identity.Clone();

            if (this.Identity != null) this.Title += " (" + this.Identity.IdentityName + ")";
        }

        /// <summary>
        /// Displays the app's main screen.
        /// </summary>
        public void Close()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
        }

        /// <summary>
        /// Saves any changes made to the affected identity.
        /// </summary>
        public async void Save()
        {
            CanSave = false;

            if (!HasChanges())
            {
                Close();
                CanSave = true;
                return;
            }


            InputSecretDialogViewModel passwordDlg = new InputSecretDialogViewModel(SecretType.Password);
            
            var dialogClosed = await passwordDlg.ShowDialog(this);
            if (dialogClosed)
            {

                if (passwordDlg.Secret == null)
                {
                    CanSave = true;
                    return;
                }

                var progress = new Progress<KeyValuePair<int, string>>();
                var progressDialog = new ProgressDialogViewModel(progress, this);
                progressDialog.ShowDialog();

                var block1Keys = await SQRL.DecryptBlock1(Identity, passwordDlg.Secret, progress);

                if (!block1Keys.DecryptionSucceeded)
                {

                    progressDialog.Close();

                   _= await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                        _loc.GetLocalizationValue("BadPasswordError"),
                        MessageBoxSize.Small, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                        .ShowDialog(this);

                    CanSave = true;
                    return;
                }

                SQRLIdentity id = await SQRL.GenerateIdentityBlock1(block1Keys.Imk, block1Keys.Ilk,
                    passwordDlg.Secret, IdentityCopy, progress, IdentityCopy.Block1.PwdVerifySeconds);


                progressDialog.Close();

                // Swap out the old type 1 block with the updated one
                // TODO: We should probably make sure that this is an atomic operation
                Identity.Blocks.Remove(Identity.Block1);
                Identity.Blocks.Insert(0, id.Block1);

                // Finally, update the identity in the db
                _identityManager.UpdateIdentity(Identity);

                CanSave = true;
                Close();
            }
        }

        /// <summary>
        /// Returns <c>true</c> if any of the identity settings were changed by the 
        /// user are and those changes have not been applied yet, or <c>false</c> otherwise.
        /// </summary>
        public bool HasChanges()
        {
            if (Identity.Block1.HintLength != IdentityCopy.Block1.HintLength) return true;
            if (Identity.Block1.PwdTimeoutMins != IdentityCopy.Block1.PwdTimeoutMins) return true;
            if (Identity.Block1.PwdVerifySeconds != IdentityCopy.Block1.PwdVerifySeconds) return true;
            if (Identity.Block1.OptionFlags.FlagsValue != IdentityCopy.Block1.OptionFlags.FlagsValue) return true;

            return false;
        }
    }
}
