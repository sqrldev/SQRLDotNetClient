using Avalonia;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.ViewModels
{
    public class NewIdentityVerifyViewModel: ViewModelBase
    {
        public NewIdentityVerifyViewModel()
        {

        }

        public NewIdentityVerifyViewModel(SQRL sqrlInstance, SQRLIdentity identity)
        {
            this.SQRLInstance = sqrlInstance;
            this.Identity = identity;
            this.Title = "SQRL Client - New Identity Verify";
        }

        public string RescueCode { get; set; }

        private int _ProgressPercentage = 0;

        private int _ProgressPercentage2 = 0;

        public int Block1ProgressPercentage { get => _ProgressPercentage; set => this.RaiseAndSetIfChanged(ref _ProgressPercentage, value); }

        public int Block2ProgressPercentage { get => _ProgressPercentage2; set => this.RaiseAndSetIfChanged(ref _ProgressPercentage2, value); }
        public int ProgressMax { get; set; } = 100;

        public string Password { get; set; }
        public SQRLIdentity Identity { get; set; }

        private SQRL SQRLInstance { get; }

        private string Message { get; } = "Re-Enter your Rescue Code from the last screen, we will use it to verify and decrypt your identity";

        public void GenerateNewIdentity()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).PriorContent;
        }


        public async void VerifyRescueCode()
        {
            var progressBlock1 = new Progress<KeyValuePair<int, string>>(percent =>
            {
                this.Block1ProgressPercentage = (int)percent.Key;
            });

            var progressBlock2 = new Progress<KeyValuePair<int, string>>(percent =>
            {
                this.Block2ProgressPercentage = ((int)percent.Key);
            });

            var data = SQRLInstance.DecryptBlock1(this.Identity, this.Password, progressBlock1);
            var dataBlock2 = SQRLInstance.DecryptBlock2(this.Identity, SQRL.CleanUpRescueCode(this.RescueCode), progressBlock2);
            await Task.WhenAll(data, dataBlock2);
            string msg = "";
            if (!data.Result.Item1)
            {
                msg = $"Invalid Password{Environment.NewLine}";
            }
            if (!dataBlock2.Result.Item1)
            {
                msg += $"Invalid Rescue Code{Environment.NewLine}";
            }
            if (!string.IsNullOrEmpty(msg))
            {
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow($"Error", $"{msg}", MessageBox.Avalonia.Enums.ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Error);
                

                await messageBoxStandardWindow.ShowDialog(AvaloniaLocator.Current.GetService<MainWindow>());
            }
            else
            {
                ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).MainMenu.currentIdentity = this.Identity;
                ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new ExportIdentityViewModel(this.SQRLInstance, this.Identity);
            }
        }
    }
}
