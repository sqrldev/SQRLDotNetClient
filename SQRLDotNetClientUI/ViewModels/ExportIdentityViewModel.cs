using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using QRCoder;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Linq;

namespace SQRLDotNetClientUI.ViewModels
{
    public class ExportIdentityViewModel: ViewModelBase
    {
        public SQRLIdentity Identity { get; }

        private Avalonia.Media.Imaging.Bitmap _qrImg;
        Avalonia.Media.Imaging.Bitmap QRImage { get { return _qrImg; } set { this.RaiseAndSetIfChanged(ref _qrImg, value); } }

        public ExportIdentityViewModel()
        {
            this.QRImage = null;
            this.Title = _loc.GetLocalizationValue("ExportIdentityWindowTitle");
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

        public async void SaveToFile()
        {
            SaveFileDialog ofd = new SaveFileDialog();

            ofd.Title = _loc.GetLocalizationValue("SaveIdentityDialogTitle");
            ofd.InitialFileName = $"{(string.IsNullOrEmpty(this.Identity.IdentityName)?"Identity":this.Identity.IdentityName)}.sqrl";
            var file = await ofd.ShowAsync(_mainWindow);
            if(!string.IsNullOrEmpty(file))
            {
                this.Identity.WriteToFile(file);
                
                await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("IdentityExportedMessageBoxTitle"),
                    string.Format(_loc.GetLocalizationValue("IdentityExportedMessageBoxText"), file),
                    MessageBoxSize.Small, MessageBoxButtons.OK,MessageBoxIcons.OK)
                    .ShowDialog(this);
            }
        }

        public async void CopyToClipboard()
        {
            var textualIdentityBytes = this.Identity.Block2.ToByteArray();
            if (this.Identity.HasBlock(3)) textualIdentityBytes = textualIdentityBytes.Concat(this.Identity.Block3.ToByteArray()).ToArray();

            string identity = SQRL.GenerateTextualIdentityBase56(textualIdentityBytes);
            await Application.Current.Clipboard.SetTextAsync(identity);
            
            await new Views.MessageBoxViewModel(_loc.GetLocalizationValue("IdentityExportedMessageBoxTitle"),
                _loc.GetLocalizationValue("IdentityCopiedToClipboardMessageBoxText"),
                MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.OK)
                .ShowDialog(this);
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
