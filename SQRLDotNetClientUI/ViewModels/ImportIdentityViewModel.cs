using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;

namespace SQRLDotNetClientUI.ViewModels
{
    public class ImportIdentityViewModel: ViewModelBase
    {
        private string _textualIdentity = "";
        public string TextualIdentity { get => _textualIdentity; set { this.RaiseAndSetIfChanged(ref _textualIdentity, value); } }

        private string _identityFile="N/A";
        public string IdentityFile { get => _identityFile; set {  this.RaiseAndSetIfChanged(ref _identityFile, value); } }

        public SQRL sqrlInstance { get; set; }

        public SQRLIdentity sqrlIdentity { get; set; }

        public string IdentityName { get; set; }

        public String Message { get; } = "Paste your Textual Identity below, or select an Identity File to Import";

        
        public ImportIdentityViewModel()
        {
            this.Title = "SQRL Client - Import Identity";
        }

        public ImportIdentityViewModel(SQRL sqrlInstance)
        {
            this.Title = "SQRL Client - Import Identity";
            this.sqrlInstance = sqrlInstance;
        }

        public async void ImportFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.AllowMultiple = false;
            FileDialogFilter fdf = new FileDialogFilter();
            fdf.Extensions.Add("sqrl");

            fdf.Name = "SQRL Identity";
            ofd.Filters.Add(fdf);
            ofd.Title = "Please select your SQRL Identity File to Import";
            var file = await ofd.ShowAsync(AvaloniaLocator.Current.GetService<MainWindow>());
            if(file.Length>0)
            {
                this.IdentityFile = file[0];
            }
        }

        public void Cancel()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = 
                ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).PriorContent;
        }

        public  async void ImportVerify()
        {
            SQRLIdentity identity = null;
            if (!string.IsNullOrEmpty(this.TextualIdentity))
            {
                try
                {
                    byte[] identityBytes = this.sqrlInstance.Base56DecodeIdentity(this.TextualIdentity);
                    bool noHeader = !SQRLIdentity.HasHeader(identityBytes);
                    identity = SQRLIdentity.FromByteArray(identityBytes, noHeader);
                }
                catch (Exception ex)
                {
                    var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                        $"Error", $"Error Importing Textual Identity: {ex.Message}", 
                        MessageBox.Avalonia.Enums.ButtonEnum.Ok, 
                        MessageBox.Avalonia.Enums.Icon.Error);

                    await messageBoxStandardWindow.ShowDialog(AvaloniaLocator.Current.GetService<MainWindow>());
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
                    var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                        $"Error", $"Error Importing Identity: {ex.Message}", 
                        MessageBox.Avalonia.Enums.ButtonEnum.Ok, 
                        MessageBox.Avalonia.Enums.Icon.Error);

                    await messageBoxStandardWindow.ShowDialog(AvaloniaLocator.Current.GetService<MainWindow>());
                }
            }

            if (identity != null)
            {
                ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = 
                    new ImportIdentitySetupViewModel(this.sqrlInstance, identity);
            }   
        }
    }
}
