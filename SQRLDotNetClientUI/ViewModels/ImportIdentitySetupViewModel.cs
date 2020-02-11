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
    public class ImportIdentitySetupViewModel : ViewModelBase
    {
        public SQRL sqrlInstance { get; set; }
        public SQRLIdentity Identity { get; set; }
        
        public string IdentityName { get; set; }
        public string Message { get; } = "Enter your New Password below, along with the Rescue Code for the imported identity";
        public string RescueCode { get; set; }
        public string NewPassword { get; set; }
        public string NewPasswordVerify { get; set; }

        private int _ProgressPercentage = 0;

        private int _ProgressPercentage2 = 0;

        private int _Block2DecryptProgressPercentage = 0;

        public int Block1ProgressPercentage { get => _ProgressPercentage; set => this.RaiseAndSetIfChanged(ref _ProgressPercentage, value); }

        public int Block2ProgressPercentage { get => _ProgressPercentage2; set => this.RaiseAndSetIfChanged(ref _ProgressPercentage2, value); }

        public int Block2DecryptProgressPercentage { get => _Block2DecryptProgressPercentage; set => this.RaiseAndSetIfChanged(ref _Block2DecryptProgressPercentage, value); }

        public int ProgressMax { get; set; } = 100;

        public ImportIdentitySetupViewModel()
        {

        }
        public ImportIdentitySetupViewModel(SQRL sqrlInstance = null, SQRLIdentity identity = null)
        {
            this.Title = "SQRL Client - Import Identity Setup";
            this.sqrlInstance = sqrlInstance;
            this.Identity = identity;
        }


        public void Previous()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).MainMenu;
        }


        public async void VerifyAndImportIdentity()
        {
            var progressBlock1 = new Progress<KeyValuePair<int, string>>(percent =>
            {
                this.Block1ProgressPercentage = (int)percent.Key;
            });

            var progressBlock2 = new Progress<KeyValuePair<int, string>>(percent =>
            {
                this.Block2ProgressPercentage = ((int)percent.Key);
            });

            var progressDecryptBlock2 = new Progress<KeyValuePair<int, string>>(percent =>
            {
                this.Block2DecryptProgressPercentage = ((int)percent.Key);
            });

            var block2Data = await this.sqrlInstance.DecryptBlock2(this.Identity, SQRL.CleanUpRescueCode(this.RescueCode), progressDecryptBlock2);
            if (block2Data.Item1)
            {

                SQRLIdentity newId = new SQRLIdentity();
                var block1 = this.sqrlInstance.GenerateIdentityBlock1(block2Data.Item2, this.NewPassword, newId, progressBlock1);
                var block2 = this.sqrlInstance.GenerateIdentityBlock2(block2Data.Item2, this.RescueCode, newId, progressBlock2);
                await Task.WhenAll(block1, block2);
                newId = block2.Result;
                if (this.Identity.Block3 != null)
                {
                    byte[] imk = this.sqrlInstance.CreateIMK(block2Data.Item2);
                    this.sqrlInstance.GenerateIdentityBlock3(block2Data.Item2, this.Identity, newId, imk, imk); 
                }
                ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).MainMenu.CurrentIdentity = newId;
                ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).MainMenu;
            }
            else
            {
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow($"Error", $"Invalid Rescue Code!", MessageBox.Avalonia.Enums.ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Error);


                await messageBoxStandardWindow.ShowDialog(AvaloniaLocator.Current.GetService<MainWindow>());
            }

        }
    }
}
