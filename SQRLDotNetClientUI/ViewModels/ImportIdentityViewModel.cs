using System;
using Avalonia.Controls;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
using SQRLUtilsLib;

namespace SQRLDotNetClientUI.ViewModels
{
    public class ImportIdentityViewModel: ViewModelBase
    {       
        private string _textualIdentity = "";
        public string TextualIdentity 
        { 
            get => _textualIdentity; 
            set { this.RaiseAndSetIfChanged(ref _textualIdentity, value); } 
        }

        private string _identityFile="N/A";
        public string IdentityFile 
        {
            get => _identityFile; 
            set { this.RaiseAndSetIfChanged(ref _identityFile, value); } 
        }
        
        public ImportIdentityViewModel()
        {
            this.Title = _loc.GetLocalizationValue("ImportIdentityWindowTitle");
        }

        public async void ImportFile()
        {
            FileDialogFilter fdf = new FileDialogFilter();
            fdf.Extensions.Add("sqrl");
            fdf.Name = _loc.GetLocalizationValue("FileDialogFilterName");

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.AllowMultiple = false;
            ofd.Filters.Add(fdf);
            ofd.Title = _loc.GetLocalizationValue("ImportOpenFileDialogTitle");

            var file = await ofd.ShowAsync(_mainWindow);

            if(file.Length>0)
            {
                this.IdentityFile = file[0];
            }
        }

        public void Cancel()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
        }

        public  async void ImportVerify()
        {
            SQRLIdentity identity = null;
            if (!string.IsNullOrEmpty(this.TextualIdentity))
            {
                try
                {
                    byte[] identityBytes = SQRL.Base56DecodeIdentity(this.TextualIdentity);
                    bool noHeader = !SQRLIdentity.HasHeader(identityBytes);
                    identity = SQRLIdentity.FromByteArray(identityBytes, noHeader);
                }
                catch (Exception ex)
                {
                    var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(
                        _loc.GetLocalizationValue("ErrorTitleGeneric"),
                        string.Format(_loc.GetLocalizationValue("TextualImportErrorMessage"), ex.Message), 
                        ButtonEnum.Ok, 
                        Icon.Error);

                    await messageBoxStandardWindow.ShowDialog(_mainWindow);
                }
            }
            else if (!string.IsNullOrEmpty(this.IdentityFile))
            {
                try
                {
                    identity = SQRLIdentity.FromFile(this.IdentityFile);
                }
                catch (Exception ex)
                {
                    var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(
                        _loc.GetLocalizationValue("ErrorTitleGeneric"),
                        string.Format(_loc.GetLocalizationValue("FileImportErrorMessage"), ex.Message),
                        ButtonEnum.Ok, 
                        Icon.Error);

                    await messageBoxStandardWindow.ShowDialog(_mainWindow);
                }
            }

            if (identity != null)
            {
                ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                    new ImportIdentitySetupViewModel(identity);
            }   
        }
    }
}
