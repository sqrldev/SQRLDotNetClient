using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;

namespace SQRLDotNetClientUI.ViewModels
{
    class IdentitySettingsViewModel : ViewModelBase
    {
        public SQRLIdentity Identity { get; set; }
        public SQRLIdentity IdentityCopy { get; set; }

        private bool _canSave = true;
        public bool CanSave 
        { 
            get => _canSave; 
            set => this.RaiseAndSetIfChanged(ref _canSave, value); 
        }

        public IdentitySettingsViewModel()
        {
            this.Title = _loc.GetLocalizationValue("IdentitySettingsDialogTitle");
            this.Identity = _identityManager.CurrentIdentity;
            this.IdentityCopy = this.Identity.Clone();

            if (this.Identity != null) this.Title += " (" + this.Identity.IdentityName + ")";
        }

        public void Close()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content =
                ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).MainMenu;
        }

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

                    await new Views.MessageBox(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                        _loc.GetLocalizationValue("BadPasswordError"),
                        MessageBoxSize.Small, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                        .ShowDialog<MessagBoxDialogResult>(_mainWindow);

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
