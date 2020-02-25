using Avalonia;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
using SQRLDotNetClientUI.AvaloniaExtensions;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    public class NewIdentityViewModel: ViewModelBase
    {
        private LocalizationExtension _loc = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService;
        private MainWindow _mainWindow = AvaloniaLocator.Current.GetService<MainWindow>();

        public SQRL SqrlInstance { get; }

        public NewIdentityViewModel()
        {
            Init();
        }
        public NewIdentityViewModel(SQRL sqrlInstance)
        {
            Init();
            this.SqrlInstance = sqrlInstance;
            this.rescueCode = SQRLUtilsLib.SQRL.FormatRescueCodeForDisplay(sqrlInstance.CreateRescueCode());
        }

        private void Init()
        {
            this.Title = _loc.GetLocalizationValue("NewIdentityWindowTitle");
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
                byte[] iuk = this.SqrlInstance.CreateIUK();
                byte[] imk = this.SqrlInstance.CreateIMK(iuk);

                var progress = new Progress<KeyValuePair<int, string>>(percent =>
                {
                    this.ProgressPercentage = (int)percent.Key / 2;
                    this.GenerationStep = percent.Value + percent.Key;
                });

                newId = this.SqrlInstance.GenerateIdentityBlock0(imk, newId);
                newId = await this.SqrlInstance.GenerateIdentityBlock1(iuk, this.Password, newId, progress);

                if (newId.Block1 != null)
                {
                    progress = new Progress<KeyValuePair<int, string>>(percent =>
                    {
                        this.ProgressPercentage = 50 + (int)(percent.Key / 2);
                        this.GenerationStep = percent.Value + percent.Key;
                    });
                    newId = await this.SqrlInstance.GenerateIdentityBlock2(iuk, SQRL.CleanUpRescueCode(this.RescueCode), newId, progress);
                    if (newId.Block2 != null)
                    {
                        this.Identity = newId;
                        ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                            new NewIdentityVerifyViewModel(this.SqrlInstance, newId);
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
