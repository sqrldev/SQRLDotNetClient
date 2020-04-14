
using Avalonia.Controls;
using ReactiveUI;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// A view model providing application logic for <c>NewIdentityView</c>.
    /// </summary>
    public class NewIdentityViewModel: ViewModelBase
    {
        private bool _canSave = true;
        private bool _passwordsMatch = true;
        private string _newPassword = "";
        private string _newPasswordVerification = "";

        /// <summary>
        /// The rescue code which was created for the new identity.
        /// </summary>
        public string RescueCode { get; }

        /// <summary>
        /// Gets or sets if its possible to hit the "OK" button on 
        /// the dialog to actually create the identity.
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
        /// The identity name that was entered by the user.
        /// </summary>
        public string IdentityName { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new <c>NewIdentityViewModel</c> intance.
        /// </summary>
        public NewIdentityViewModel()
        {
            this.Title = _loc.GetLocalizationValue("NewIdentityWindowTitle");
            this.RescueCode = SQRL.FormatRescueCodeForDisplay(SQRL.CreateRescueCode());
        }

        /// <summary>
        /// Actually generates the new identity.
        /// </summary>
        public async void GenerateNewIdentity()
        {
            SQRLIdentity newId = new SQRLIdentity(this.IdentityName);
            byte[] iuk = SQRL.CreateIUK();
            byte[] imk = SQRL.CreateIMK(iuk);

            var progressBlock1 = new Progress<KeyValuePair<int, string>>();
            var progressBlock2 = new Progress<KeyValuePair<int, string>>();

            var progressDialog = new ProgressDialogViewModel(new List<Progress<KeyValuePair<int, string>>>() {
                progressBlock1, progressBlock2}, this);
            
            
            progressDialog.ShowDialog();

            newId = SQRL.GenerateIdentityBlock0(imk, newId);
            newId = await SQRL.GenerateIdentityBlock1(iuk, this.NewPassword, newId, progressBlock1);

            if (newId.Block1 != null)
            {
                newId = await SQRL.GenerateIdentityBlock2(iuk, SQRL.CleanUpRescueCode(this.RescueCode), newId, progressBlock2);
                if (newId.Block2 != null)
                {
                    
                    progressDialog.Close();
                    

                    ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                        new NewIdentityVerifyViewModel(newId, this.NewPassword);
                }
            }
            
            progressDialog.Close();
        }

        /// <summary>
        /// Displays the previous screen.
        /// </summary>
        public void Cancel()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
        }

        /// <summary>
        /// Opens a file save dialog and saves the rescue code pdf document
        /// in the location chosen by the user.
        /// </summary>
        public async void SaveRescueCodePdf()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            FileDialogFilter fdf = new FileDialogFilter
            {
                Name = "PDF files (.pdf)",
                Extensions = new List<string> { "pdf" }
            };

            sfd.Title = _loc.GetLocalizationValue("SaveRescueCodeDialogTitle");
            sfd.InitialFileName = $"RescueCode.pdf";
            sfd.Filters.Add(fdf);
            var file = await sfd.ShowAsync(_mainWindow);

            if (string.IsNullOrEmpty(file)) return;

            try
            {
                PdfHelper.CreateRescueCodeDocument(file, this.RescueCode, this.IdentityName);
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = file,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                await new MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"), ex.Message,
                    MessageBoxSize.Small, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                    .ShowDialog(this);
            }
        }
    }
}
