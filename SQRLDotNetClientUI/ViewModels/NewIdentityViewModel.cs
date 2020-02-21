using Avalonia;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    public class NewIdentityViewModel: ViewModelBase
    {
        public SQRL sqrlInstance { get; }


        public NewIdentityViewModel()
        {
            
        }
        public NewIdentityViewModel(SQRL sqrlInstance)
        {
            this.sqrlInstance = sqrlInstance;
            this.rescueCode = SQRLUtilsLib.SQRL.FormatRescueCodeForDisplay(sqrlInstance.CreateRescueCode());
            this.Title = "SQRL Client - New Identity";
        }
        public string Message => "To generate a new identity you need to save this rescue code shown below and enter a password. You REALLY need to save this rescue code, we will test you later!";

        private string rescueCode;
        public string RescueCode { get { return rescueCode; } }

        public string Password { get; set; } = string.Empty;

        public string PasswordConfirm { get; set; } = string.Empty;

        public string IdentityName { get; set; } = string.Empty;

        private int _ProgressPercentage = 0;

        public int ProgressPercentage { get => _ProgressPercentage; set => this.RaiseAndSetIfChanged(ref _ProgressPercentage, value); }


        public int ProgressMax { get; set; } = 100;

        public string GenerationStep { get; set; }

        private SQRLIdentity Identity { get; set; }

        public async void GenerateNewIdentity()
        {
            if (this.Password.Equals(this.PasswordConfirm))
            {

                SQRLIdentity newId = new SQRLIdentity(this.IdentityName);
                byte[] iuk = this.sqrlInstance.CreateIUK();
                byte[] imk = this.sqrlInstance.CreateIMK(iuk);

                var progress = new Progress<KeyValuePair<int, string>>(percent =>
                {
                    this.ProgressPercentage = (int)percent.Key / 2;
                    this.GenerationStep = percent.Value + percent.Key;
                });

                newId = this.sqrlInstance.GenerateIdentityBlock0(imk, newId);
                newId = await this.sqrlInstance.GenerateIdentityBlock1(iuk, this.Password, newId, progress);

                if (newId.Block1 != null)
                {
                    progress = new Progress<KeyValuePair<int, string>>(percent =>
                    {
                        this.ProgressPercentage = 50 + (int)(percent.Key / 2);
                        this.GenerationStep = percent.Value + percent.Key;
                    });
                    newId = await this.sqrlInstance.GenerateIdentityBlock2(iuk, SQRL.CleanUpRescueCode(this.RescueCode), newId, progress);
                    if (newId.Block2 != null)
                    {
                        this.Identity = newId;
                        ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new NewIdentityVerifyViewModel(this.sqrlInstance, newId);
                        /*AvaloniaLocator.Current.GetService<NewIdentityWindow>().Hide();
                        NewIdentityVerifyWindow idV = new NewIdentityVerifyWindow(this.sqrlInstance, newId);
                        await idV.ShowDialog(AvaloniaLocator.Current.GetService<NewIdentityWindow>());*/
                    }
                }
            }
            else
            {
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error, Passwords don't match!", "Error!", MessageBox.Avalonia.Enums.ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Error);

                await messageBoxStandardWindow.Show();
            }
        }

        public  void Cancel()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).PriorContent;
        }
    }

}
