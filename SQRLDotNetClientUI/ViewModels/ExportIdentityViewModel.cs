using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using QRCoder;
using ReactiveUI;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            var textualIdentityBytes = this.Identity.ToByteArray(includeHeader: true, minimumSize: true);
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(textualIdentityBytes, QRCodeGenerator.ECCLevel.H);
            QRCode qrCode = new QRCode(qrCodeData);
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var bpm = new System.Drawing.Bitmap(assets.Open(new Uri("resm:SQRLDotNetClientUI.Assets.SQRL_icon_normal_32.png")));
            var bitMap = qrCode.GetGraphic(3, System.Drawing.Color.Black, System.Drawing.Color.White, bpm, 15, 1);
            
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
                
                await new MessageBox(_loc.GetLocalizationValue("IdentityExportedMessageBoxTitle"),
                    string.Format(_loc.GetLocalizationValue("IdentityExportedMessageBoxText"), file),
                    MessageBoxSize.Small, MessageBoxButtons.OK,MessageBoxIcons.OK)
                    .ShowDialog(this);
            }
        }

        public async void CopyToClipboard()
        {
            var textualIdentityBytes = this.Identity.ToByteArray(includeHeader: true, minimumSize: true);

            string identity = SQRL.GenerateTextualIdentityBase56(textualIdentityBytes);
            await Application.Current.Clipboard.SetTextAsync(identity);
            
            await new MessageBox(_loc.GetLocalizationValue("IdentityExportedMessageBoxTitle"),
                _loc.GetLocalizationValue("IdentityCopiedToClipboardMessageBoxText"),
                MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.OK)
                .ShowDialog(this);
        }

        public async void SaveAsPdf()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            FileDialogFilter fdf = new FileDialogFilter
            {
                Name = "PDF files (.pdf)",
                Extensions = new List<string> { "pdf" }
            };

            sfd.Title = _loc.GetLocalizationValue("SaveIdentityDialogTitle");
            sfd.InitialFileName = $"{(string.IsNullOrEmpty(this.Identity.IdentityName) ? "Identity" : this.Identity.IdentityName)}.pdf";
            sfd.Filters.Add(fdf);
            var file = await sfd.ShowAsync(_mainWindow);

            if (string.IsNullOrEmpty(file)) return;

            try
            {
                PdfHelper.CreateIdentityDocument(file, this.Identity); 
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = file,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex) 
            {
                await new MessageBox(_loc.GetLocalizationValue("ErrorTitleGeneric"), ex.Message,
                    MessageBoxSize.Small, MessageBoxButtons.OK, MessageBoxIcons.OK)
                    .ShowDialog<MessagBoxDialogResult>(_mainWindow);
            }
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
