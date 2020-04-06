using Avalonia.Controls;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// ViewModel used for re-keying the current identity.
    /// </summary>
    public class ReKeyViewModel : ViewModelBase
    {
        private bool _canSave = true;
        private bool _passwordsMatch = true;
        private string _password = "";
        private string _newPassword = "";
        private string _newPasswordVerification = "";

        /// <summary>
        /// Gets or sets if its possible to hit the "OK" button on 
        /// the dialog to actually rekey the identity.
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
        /// Creates a new <c>ReKeyViewModel</c> instance.
        /// </summary>
        public ReKeyViewModel()
        {
            if (_loc != null)
            {
                this.Title = string.Format(_loc.GetLocalizationValue("ReKeyIdentityTitle"),
                    _identityManager.CurrentIdentity.IdentityName);
            }
        }

        /// <summary>
        /// Displays the main screen.
        /// </summary>
        public void Cancel()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
        }

        /// <summary>
        /// Runs the Re-Key logic.
        /// </summary>
        public async void Next()
        {
            //Label used for retrying the rescue code if you type it wrong the first time
            RetryRescueCode:

            //Dialog Box used to capture the user's current Rescue Code
            InputSecretDialogViewModel rescueCodeDlg = new InputSecretDialogViewModel(SecretType.RescueCode);
            //rescueCodeDlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var dialogClosed = await rescueCodeDlg.ShowDialog(this);
            if (dialogClosed)
            {
                //List of Progress Reporters to be used during the re-key process (progress bar magic)
                var progressList = new List<Progress<KeyValuePair<int, string>>>() {
                new Progress<KeyValuePair<int, string>>(), new Progress<KeyValuePair<int, string>>() };

                //Progress Dialog will show our "Progress" as the Identity is Decrypted, and Re-Encrypted for Rekey
                var progressDialog = new ProgressDialogViewModel(progressList, this, true, true);

                progressDialog.ShowDialog();

                //Actually do the Re-Key Work
                var result = await SQRL.RekeyIdentity(_identityManager.CurrentIdentity, SQRL.CleanUpRescueCode(rescueCodeDlg.Secret),
                    NewPassword, progressList[0], progressList[1]);

                if (!result.Success)
                {

                    progressDialog.Close();

                    //Fail bad rescue code (something went wrong...) try again?
                    var answer = await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                        _loc.GetLocalizationValue("InvalidRescueCodeMessage"),
                        MessageBoxSize.Small, MessageBoxButtons.YesNo, MessageBoxIcons.ERROR)
                        .ShowDialog(this);

                    if (answer == MessagBoxDialogResult.YES)
                    {
                        goto RetryRescueCode; //Go back up and re-do it all, this time with passion!
                    }
                }
                else if (result.Success) //All Good
                {

                    progressDialog.Close();

                    //This label is used to re-share the new rescue code if it was copied incorrectly.
                    CopiedWrong:
                    //Message Box which displays the new Rescue Code to the user
                   _= await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("IdentityReKeyNewCode"),
                        string.Format(_loc.GetLocalizationValue("IdentityReKeyMessage"), SQRL.FormatRescueCodeForDisplay(result.NewRescueCode)),
                        MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.OK)
                        .ShowDialog(this);

                    //Ask the user to re-type their New Rescue Code to verify that they copied it correctly.
                    rescueCodeDlg = new InputSecretDialogViewModel(SecretType.RescueCode);

                    dialogClosed = await rescueCodeDlg.ShowDialog(this);
                    if (dialogClosed)
                    {
                        //New progress dialog for the verification step
                        progressDialog = new ProgressDialogViewModel(progressList[0], this, false);

                        progressDialog.ShowDialog();

                        //Decrypt Block 2 to verify they copied their rescue code correctly.
                        var block2Results = await SQRL.DecryptBlock2(result.RekeyedIdentity, rescueCodeDlg.Secret, progressList[0]);
                        if (block2Results.DecryptionSucceeded) //All Good, All Done
                        {

                            progressDialog.Close();
                            _identityManager.DeleteCurrentIdentity();
                            _identityManager.ImportIdentity(result.RekeyedIdentity, true);

                            // Display the main screen
                            Cancel();
                        }
                        else //Fail bad rescue code... try again?
                        {

                            progressDialog.Close();
                            var answer = await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                                _loc.GetLocalizationValue("InvalidRescueCodeMessage"),
                                MessageBoxSize.Small, MessageBoxButtons.YesNo, MessageBoxIcons.ERROR)
                                .ShowDialog(this);

                            if (answer == MessagBoxDialogResult.YES)
                            {
                                goto CopiedWrong; //Try Again
                            }
                            else //Abort the whole thing
                            {
                                _ = await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                                    _loc.GetLocalizationValue("IdentityReKeyFailed"),
                                    MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                                    .ShowDialog(this);
                            }
                        }
                    }

                }

                // Display the main screen
                Cancel();
            }
        }
    }
}
