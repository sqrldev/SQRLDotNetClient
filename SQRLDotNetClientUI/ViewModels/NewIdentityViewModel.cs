using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
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
            this.rescueCode = SQRL.FormatRescueCodeForDisplay(SQRL.CreateRescueCode());
        }

        private string rescueCode;
        public string RescueCode { get { return rescueCode; } }

        public string Password { get; set; } = string.Empty;

        public string PasswordConfirm { get; set; } = string.Empty;

        public string IdentityName { get; set; } = string.Empty;

        private int _ProgressPercentage = 0;

        public int ProgressPercentage 
        { 
            get => _ProgressPercentage; 
            set => this.RaiseAndSetIfChanged(ref _ProgressPercentage, value); 
        }

        public int ProgressMax { get; set; } = 100;

        public string GenerationStep { get; set; }

        private SQRLIdentity Identity { get; set; }

        public async void GenerateNewIdentity()
        {
            if (this.Password.Equals(this.PasswordConfirm))
            {

                SQRLIdentity newId = new SQRLIdentity(this.IdentityName);
                byte[] iuk = SQRL.CreateIUK();
                byte[] imk = SQRL.CreateIMK(iuk);

                var progress = new Progress<KeyValuePair<int, string>>(percent =>
                {
                    this.ProgressPercentage = (int)percent.Key / 2;
                    this.GenerationStep = percent.Value + percent.Key;
                });

                newId = SQRL.GenerateIdentityBlock0(imk, newId);
                newId = await SQRL.GenerateIdentityBlock1(iuk, this.Password, newId, progress);

                if (newId.Block1 != null)
                {
                    progress = new Progress<KeyValuePair<int, string>>(percent =>
                    {
                        this.ProgressPercentage = 50 + (int)(percent.Key / 2);
                        this.GenerationStep = percent.Value + percent.Key;
                    });
                    newId = await SQRL.GenerateIdentityBlock2(iuk, SQRL.CleanUpRescueCode(this.RescueCode), newId, progress);
                    if (newId.Block2 != null)
                    {
                        this.Identity = newId;
                        ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                            new NewIdentityVerifyViewModel(newId);
                    }
                }
            }
            else
            {
                var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(
                    _loc.GetLocalizationValue("ErrorTitleGeneric"),
                    _loc.GetLocalizationValue("PasswordsDontMatchErrorMessage"),
                    ButtonEnum.Ok, Icon.Error);

                await messageBoxStandardWindow.ShowDialog(_mainWindow);
            }
        }

        public  void Cancel()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
        }
    }
}
