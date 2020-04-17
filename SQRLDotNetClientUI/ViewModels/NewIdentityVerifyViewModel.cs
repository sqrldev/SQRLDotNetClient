
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
    /// A view model representing the app's "New Identity Verification" screen.
    /// </summary>
    public class NewIdentityVerifyViewModel: ViewModelBase
    {
        /// <summary>
        /// Gets or sets the secret rescue code entered by the user.
        /// </summary>
        public string RescueCode { get; set; }

        /// <summary>
        /// Gets or sets the secret identity master password set by the user.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the newly created identity to be verified.
        /// </summary>
        public SQRLIdentity Identity { get; set; }

        /// <summary>
        /// Creates a new <c>NewIdentityVerifyViewModel</c> intance and performs
        /// some initialization tasks.
        /// </summary>
        public NewIdentityVerifyViewModel()
        {
            Init();
        }

        /// <summary>
        /// Creates a new <c>NewIdentityVerifyViewModel</c> intance and performs
        /// some initialization tasks.
        /// </summary>
        /// <param name="identity">The newly created identity to be verified.</param>
        /// <param name="password">The identity's master password set by the user.</param>
        public NewIdentityVerifyViewModel(SQRLIdentity identity, string password)
        {
            Init();
            this.Identity = identity;
            this.Password = password;
        }

        /// <summary>
        /// Performs initialization tasks such as setting the window title.
        /// </summary>
        private void Init()
        {
            this.Title = _loc.GetLocalizationValue("NewIdentityVerifyWindowTitle");
        }

        /// <summary>
        /// Navigates back to the previous "Create new identity" screen.
        /// </summary>
        public void GenerateNewIdentity()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
        }

        /// <summary>
        /// Verifies whether the user has copied down and entered the correct rescue code,
        /// and, upon successful verification, imports the identity into the database and
        /// navigates to the "Export Identity" screen.
        /// </summary>
        public async void VerifyRescueCode()
        {
            var progressBlock1 = new Progress<KeyValuePair<int, string>>();
            var progressBlock2 = new Progress<KeyValuePair<int, string>>();
            var progressDialog = new ProgressDialogViewModel(new List<Progress<KeyValuePair<int, string>>>() {
                    progressBlock1, progressBlock2},this);
            progressDialog.ShowDialog();
            

            var block1Task = SQRL.DecryptBlock1(this.Identity, this.Password, progressBlock1);
            var block2Task = SQRL.DecryptBlock2(this.Identity, SQRL.CleanUpRescueCode(this.RescueCode), progressBlock2);
            await Task.WhenAll(block1Task, block2Task);

            
            progressDialog.Close();

            string msg = "";
            if (!block1Task.Result.DecryptionSucceeded) msg = _loc.GetLocalizationValue("InvalidPasswordMessage") + Environment.NewLine;
            if (!block2Task.Result.DecryptionSucceeded) msg = _loc.GetLocalizationValue("InvalidRescueCodeMessage") + Environment.NewLine;

            if (!string.IsNullOrEmpty(msg))
            {
                _=await new MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"), $"{msg}", 
                    MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                    .ShowDialog(this);
            }
            else
            {
                try
                {
                    _identityManager.ImportIdentity(this.Identity, true);
                }
                catch (InvalidOperationException e)
                {
                    await new MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                        e.Message, MessageBoxSize.Medium,
                        MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                        .ShowDialog(this);
                }
                finally
                {
                    ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                        new ExportIdentityViewModel();
                }
            }
        }
    }
}
