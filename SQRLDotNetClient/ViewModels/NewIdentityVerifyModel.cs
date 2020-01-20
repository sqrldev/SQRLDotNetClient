using Avalonia;
using ReactiveUI;
using SQRLDotNetClient.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClient.ViewModels
{

    public class NewIdentityVerifyModel : ViewModelBase
    {

        public NewIdentityVerifyModel()
        {

        }

        public NewIdentityVerifyModel(SQRL sqrlInstance, SQRLIdentity identity)
        {
            this.SQRLInstance = sqrlInstance;
            this.Identity = identity;
        }

        public string RescueCode { get; set; }

        private int _ProgressPercentage = 0;

        public int ProgressPercentage { get => _ProgressPercentage; set => this.RaiseAndSetIfChanged(ref _ProgressPercentage, value); }
        public int ProgressMax { get; set; } = 100;

        public string Password { get; set; }
        public SQRLIdentity Identity { get; set; }

        private SQRL SQRLInstance { get; }

        private string Message { get; } = "Re-Enter your Rescue Code from the last screen, we will use it to verify and decrypt your identity";

        public void GenerateNewIdentity()
        {
            if (AvaloniaLocator.Current.GetService<NewIdentityWindow>() != null)
            {
                AvaloniaLocator.Current.GetService<NewIdentityVerifyWindow>().Hide();
                AvaloniaLocator.Current.GetService<NewIdentityWindow>().Show();
            }
        }


        public async void VerifyRescueCode()
        {
            var progress = new Progress<KeyValuePair<int, string>>(percent =>
            {
                this.ProgressPercentage = (int)percent.Key / 2;
            });

            var data = await SQRLInstance.DecryptBlock1(this.Identity, this.Password, progress);
            string msg = "";
            if(!data.Item1)
            {
                msg = $"Invalid Password{Environment.NewLine}";
            }
            progress = new Progress<KeyValuePair<int, string>>(percent =>
            {
                this.ProgressPercentage = 50+ ((int)percent.Key / 2);
            });
            var dataBlock2 = await SQRLInstance.DecryptBlock2(this.Identity,  SQRL.CleanUpRescueCode(this.RescueCode), progress);
            if(!dataBlock2.Item1)
            {
                msg += $"Invalid Rescue Code{Environment.NewLine}";
            }
            if(!string.IsNullOrEmpty(msg))
            {
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow($"{msg}!", "Error!", MessageBox.Avalonia.Enums.ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Error);

                await messageBoxStandardWindow.Show();
            }
        }
    }
}
