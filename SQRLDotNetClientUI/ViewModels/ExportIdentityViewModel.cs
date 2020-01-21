using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using QRCoder;
using ReactiveUI;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;

using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    public class ExportIdentityViewModel: ViewModelBase
    {
        SQRL sqrlInstance { get; }
        SQRLIdentity Identity { get; }

        private Avalonia.Media.Imaging.Bitmap _qrImg;
        Avalonia.Media.Imaging.Bitmap QRImage { get { return _qrImg; } set { this.RaiseAndSetIfChanged(ref _qrImg, value); } }

        public ExportIdentityViewModel(SQRL sqrlInstance, SQRLIdentity identity)
        {
            this.sqrlInstance = sqrlInstance;
            this.Identity = identity;
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(this.Identity.ToByteArray(), QRCodeGenerator.ECCLevel.H);
            QRCode qrCode = new QRCode(qrCodeData);
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var bpm = new System.Drawing.Bitmap(assets.Open(new Uri("resm:SQRLDotNetClientUI.Assets.SQRL_icon_normal_256.png")));
            var bitMap = qrCode.GetGraphic(20, System.Drawing.Color.Black, System.Drawing.Color.White, bpm);
            var temp = System.IO.Path.GetTempFileName();
            bitMap.Save(temp);
            
            this.QRImage = new Avalonia.Media.Imaging.Bitmap(temp);

        }

        public ExportIdentityViewModel()
        {
            this.QRImage = new Avalonia.Media.Imaging.Bitmap(@"C:\Users\jose\AppData\Local\Temp\tmp5572.bmp");
        }
    }
}
