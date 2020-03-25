
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;

namespace SQRLDotNetClientUI.ViewModels
{
    public class NewIdentityViewModel: ViewModelBase
    {
        public NewIdentityViewModel()
        {
            this.Title = _loc.GetLocalizationValue("NewIdentityWindowTitle");
            this.RescueCode = SQRL.FormatRescueCodeForDisplay(SQRL.CreateRescueCode());
        }

        public string RescueCode { get; }

        public string Password { get; set; } = string.Empty;

        public string PasswordConfirm { get; set; } = string.Empty;

        public string IdentityName { get; set; } = string.Empty;

        public async void GenerateNewIdentity()
        {
            if (this.Password.Equals(this.PasswordConfirm))
            {

                SQRLIdentity newId = new SQRLIdentity(this.IdentityName);
                byte[] iuk = SQRL.CreateIUK();
                byte[] imk = SQRL.CreateIMK(iuk);

                var progressBlock1 = new Progress<KeyValuePair<int, string>>();
                var progressBlock2 = new Progress<KeyValuePair<int, string>>();
                var progressDialog = new ProgressDialog(new List<Progress<KeyValuePair<int, string>>>() {
                    progressBlock1, progressBlock2});
                _ = progressDialog.ShowDialog(_mainWindow);

                newId = SQRL.GenerateIdentityBlock0(imk, newId);
                newId = await SQRL.GenerateIdentityBlock1(iuk, this.Password, newId, progressBlock1);

                if (newId.Block1 != null)
                {
                    newId = await SQRL.GenerateIdentityBlock2(iuk, SQRL.CleanUpRescueCode(this.RescueCode), newId, progressBlock2);
                    if (newId.Block2 != null)
                    {
                        progressDialog.Close();

                        ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                            new NewIdentityVerifyViewModel(newId, this.Password);
                    }
                }
                progressDialog.Close();
            }
            else
            {
                await new Views.MessageBox(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                    _loc.GetLocalizationValue("PasswordsDontMatchErrorMessage"),
                    MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                    .ShowDialog<MessagBoxDialogResult>(_mainWindow);
            }
        }

        public  void Cancel()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
        }
    }
}
