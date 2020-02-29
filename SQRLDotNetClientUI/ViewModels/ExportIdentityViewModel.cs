using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using QRCoder;
using ReactiveUI;
using SQRLDotNetClientUI.Models;
using SQRLUtilsLib;
using System;

namespace SQRLDotNetClientUI.ViewModels
{
    public class ExportIdentityViewModel: ViewModelBase
    {
        private IdentityManager _identityManager = IdentityManager.Instance;

        public SQRL sqrlInstance { get; }
        public SQRLIdentity Identity { get; }

        private Avalonia.Media.Imaging.Bitmap _qrImg;
        Avalonia.Media.Imaging.Bitmap QRImage { get { return _qrImg; } set { this.RaiseAndSetIfChanged(ref _qrImg, value); } }

        public ExportIdentityViewModel(SQRL sqrlInstance)
        {
            try
            {
                
                Init();
                this.sqrlInstance = sqrlInstance;
                this.Identity = _identityManager.CurrentIdentity;
                
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(this.Identity.ToByteArray(), QRCodeGenerator.ECCLevel.H);
                
                QRCode qrCode = new QRCode(qrCodeData);
                var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                
                var bpm = new System.Drawing.Bitmap(assets.Open(new Uri("resm:SQRLDotNetClientUI.Assets.SQRL_icon_normal_32.png")));
                var bitMap = qrCode.GetGraphic(3, System.Drawing.Color.Black, System.Drawing.Color.White, bpm,15,1);
                
                var temp = System.IO.Path.GetTempFileName();
                bitMap.Save(temp);
                
                this.QRImage = new Avalonia.Media.Imaging.Bitmap(temp);
                
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public ExportIdentityViewModel()
        {
            Init();
        }

        private void Init()
        {
            this.QRImage = null;
            this.Title = _loc.GetLocalizationValue("ExportIdentityWindowTitle");
        }

        public async void SaveToFile()
        {
            SaveFileDialog ofd = new SaveFileDialog();

            ofd.Title = _loc.GetLocalizationValue("SaveIdentityDialogTitle");
            ofd.InitialFileName = $"{(string.IsNullOrEmpty(this.Identity.IdentityName)?"Identity":this.Identity.IdentityName)}.sqrl";
            var file = await ofd.ShowAsync(_mainWindow);
            if(!string.IsNullOrEmpty(file))
            {
                this.Identity.WriteToFile(file);
                var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(
                    _loc.GetLocalizationValue("IdentityExportedMessageBoxTitle"), 
                    string.Format(_loc.GetLocalizationValue("IdentityExportedMessageBoxText"), file), 
                    ButtonEnum.Ok, 
                    Icon.Success);

                await messageBoxStandardWindow.ShowDialog(_mainWindow);
            }
            else
            {
                
            }
        }

        public async void CopyToClipboard()
        {
            string identity = this.sqrlInstance.GenerateTextualIdentityBase56(this.Identity.ToByteArray());
            await Application.Current.Clipboard.SetTextAsync(identity);

            var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(
                _loc.GetLocalizationValue("IdentityExportedMessageBoxTitle"),
                _loc.GetLocalizationValue("IdentityCopiedToClipboardMessageBoxText"), 
                ButtonEnum.Ok, 
                Icon.Success);

            await messageBoxStandardWindow.ShowDialog(_mainWindow);
        }

        public void Back()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
        }

        public void Done()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
        }
    }
}
