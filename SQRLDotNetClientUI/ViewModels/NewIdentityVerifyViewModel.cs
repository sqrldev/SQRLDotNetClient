
using ReactiveUI;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.ViewModels
{
    public class NewIdentityVerifyViewModel: ViewModelBase
    {       
        public NewIdentityVerifyViewModel()
        {
            Init();
        }

        public NewIdentityVerifyViewModel(SQRLIdentity identity, string password)
        {
            Init();
            this.Identity = identity;
            this.Password = password;
        }

        private void Init()
        {
            this.Title = _loc.GetLocalizationValue("NewIdentityVerifyWindowTitle");
        }

        public string RescueCode { get; set; }

        public string Password { get; set; }

        public SQRLIdentity Identity { get; set; }

        public void GenerateNewIdentity()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
        }

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
                    await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"),
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
