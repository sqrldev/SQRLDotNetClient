using Avalonia;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
using SQRLDotNetClientUI.AvaloniaExtensions;
using SQRLDotNetClientUI.Models;
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
        private IdentityManager _identityManager = IdentityManager.Instance;
        private LocalizationExtension _loc = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService;
        private MainWindow _mainWindow = AvaloniaLocator.Current.GetService<MainWindow>();

        public NewIdentityVerifyViewModel()
        {
            Init();
        }

        public NewIdentityVerifyViewModel(SQRL sqrlInstance, SQRLIdentity identity)
        {
            Init();
            this.SQRLInstance = sqrlInstance;
            this.Identity = identity;
        }

        private void Init()
        {
            this.Title = _loc.GetLocalizationValue("NewIdentityVerifyWindowTitle");
        }

        public string RescueCode { get; set; }

        private int _ProgressPercentage = 0;

        private int _ProgressPercentage2 = 0;

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
        public int ProgressMax { get; set; } = 100;

        public string Password { get; set; }
        public SQRLIdentity Identity { get; set; }

        private SQRL SQRLInstance { get; }

        public void GenerateNewIdentity()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
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
            if (!data.Result.Item1) msg = _loc.GetLocalizationValue("InvalidPasswordMessage") + Environment.NewLine;
            if (!dataBlock2.Result.Item1) msg = _loc.GetLocalizationValue("InvalidRescueCodeMessage") + Environment.NewLine;

            if (!string.IsNullOrEmpty(msg))
            {
                var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(
                    _loc.GetLocalizationValue("ErrorTitleGeneric"),
                    $"{msg}", 
                    ButtonEnum.Ok, 
                    Icon.Error);

                await messageBoxStandardWindow.ShowDialog(_mainWindow);
            }
            else
            {
                try
                {
                    _identityManager.ImportIdentity(this.Identity, true);
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
                        new ExportIdentityViewModel(this.SQRLInstance);
                }
            }
        }
    }
}
