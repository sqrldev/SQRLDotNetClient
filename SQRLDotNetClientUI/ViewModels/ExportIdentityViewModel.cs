using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using QRCoder;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;

using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    public class ExportIdentityViewModel: ViewModelBase
    {
        public string Message { get; } = "To export your identity, either scan the QR Code with your Other Client, Save it to a File or Copy it to your clippboard";
        SQRL sqrlInstance { get; }
        SQRLIdentity Identity { get; }

        private Avalonia.Media.Imaging.Bitmap _qrImg;
        Avalonia.Media.Imaging.Bitmap QRImage { get { return _qrImg; } set { this.RaiseAndSetIfChanged(ref _qrImg, value); } }

        public ExportIdentityViewModel(SQRL sqrlInstance, SQRLIdentity identity)
        {
            this.sqrlInstance = sqrlInstance;
            this.Identity = identity;
            this.Title = "SQRL Client - Export Identity";
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

        public ExportIdentityViewModel()
        {
            this.QRImage = new Avalonia.Media.Imaging.Bitmap(@"C:\Users\jose\AppData\Local\Temp\tmpC441.bmp");
        }

        public async void SaveToFile()
        {
            SaveFileDialog ofd = new SaveFileDialog();

            ofd.Title = "Select a location and name to save your Identity";
            ofd.InitialFileName = $"{(string.IsNullOrEmpty(this.Identity.IdentityName)?"Identity":this.Identity.IdentityName)}.sqrl";
            var file = await ofd.ShowAsync(AvaloniaLocator.Current.GetService<MainWindow>());
            if(!string.IsNullOrEmpty(file))
            {
                this.Identity.WriteToFile(file);
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow($"Done", $"Identity has been exported to the selectd file: {file}", MessageBox.Avalonia.Enums.ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Success);

                await messageBoxStandardWindow.Show();
            }
            else
            {
                
            }
        }

        public async void CopyToClippboard()
        {
            string identity = this.sqrlInstance.GenerateTextualIdentityBase56(this.Identity.ToByteArray());
            await Avalonia.Application.Current.Clipboard.SetTextAsync(identity);
            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow($"Done", $"Your identity has been copied to the Operating System's Clipboard, you may now paste it into a different client to import it. (You will need your rescue code!)", MessageBox.Avalonia.Enums.ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Success);

            await messageBoxStandardWindow.Show();
        }

        public void Back()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).PriorContent;
        }

        public void Done()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).MainMenu;
        }
    }
}
