using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
using SQRLDotNetClientUI.Models;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.ViewModels
{ 
    public class ImportIdentitySetupViewModel : ViewModelBase
    {
        private IdentityManager _identityManager = IdentityManager.Instance;

        public SQRL sqrlInstance { get; set; }
        public SQRLIdentity Identity { get; set; }
        public string IdentityName { get; set; } = "";
        public string RescueCode { get; set; }
        public string NewPassword { get; set; }
        public string NewPasswordVerify { get; set; }

        private int _ProgressPercentage = 0;

        private int _ProgressPercentage2 = 0;

        private int _Block2DecryptProgressPercentage = 0;

        public int Block1ProgressPercentage 
        { 
            get => _ProgressPercentage; 
            set => this.RaiseAndSetIfChanged(ref _ProgressPercentage, value); 
        }

        public int Block2ProgressPercentage 
        { 
            get => _ProgressPercentage2; 
            set => this.RaiseAndSetIfChanged(ref _ProgressPercentage2, value); 
        }

        public int Block2DecryptProgressPercentage 
        { 
            get => _Block2DecryptProgressPercentage; 
            set => this.RaiseAndSetIfChanged(ref _Block2DecryptProgressPercentage, value); 
        }

        public int ProgressMax { get; set; } = 100;

        public ImportIdentitySetupViewModel()
        {
            Init();
        }
        public ImportIdentitySetupViewModel(SQRL sqrlInstance, SQRLIdentity identity)
        {
            Init();
            this.sqrlInstance = sqrlInstance;
            this.Identity = identity;
        }

        private void Init()
        {
            this.Title = _loc.GetLocalizationValue("ImportIdentitySetupWindowTitle");
        }

        public void Previous()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
        }

        public void Cancel()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
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

            (bool ok, byte[] iuk, string errorMsg) = await this.sqrlInstance.DecryptBlock2(
                this.Identity, SQRL.CleanUpRescueCode(this.RescueCode), progressDecryptBlock2);

            if (ok)
            {
                SQRLIdentity newId = new SQRLIdentity();
                byte[] imk = this.sqrlInstance.CreateIMK(iuk);

                if (this.Identity.HasBlock(0)) newId.Blocks.Add(this.Identity.Block0);
                else this.sqrlInstance.GenerateIdentityBlock0(imk, newId);
                var block1 = this.sqrlInstance.GenerateIdentityBlock1(iuk, this.NewPassword, newId, progressBlock1);
                var block2 = this.sqrlInstance.GenerateIdentityBlock2(iuk, this.RescueCode, newId, progressBlock2);
                await Task.WhenAll(block1, block2);

                newId = block2.Result;
                if (this.Identity.Block3 != null)
                {
                    this.sqrlInstance.GenerateIdentityBlock3(iuk, this.Identity, newId, imk, imk); 
                }
                newId.IdentityName = this.IdentityName;

                try
                {
                    _identityManager.ImportIdentity(newId, true);
                }
                catch (InvalidOperationException e)
                {
                    var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(
                    _loc.GetLocalizationValue("ErrorTitleGeneric"),
                    e.Message,
                    ButtonEnum.Ok,
                    Icon.Error);

                    await messageBoxStandardWindow.ShowDialog(_mainWindow);
                }
                finally
                {
                    ((MainWindowViewModel)_mainWindow.DataContext).Content =
                    ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
                }
            }
            else
            {
                var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(
                    _loc.GetLocalizationValue("ErrorTitleGeneric"),
                    _loc.GetLocalizationValue("InvalidRescueCodeMessage"), 
                    ButtonEnum.Ok, 
                    Icon.Error);

                await messageBoxStandardWindow.ShowDialog(_mainWindow);
            }
        }
    }
}
