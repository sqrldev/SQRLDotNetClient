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
using System.IO;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// A view model which handles various different ways of exporting 
    /// the currently active SQRL identity.
    /// </summary>
    public class ExportIdentityViewModel: ViewModelBase
    {
        private Avalonia.Media.Imaging.Bitmap _qrImage;

        /// <summary>
        /// Gets or sets a bitmap representing the identity as a QR-code.
        /// </summary>
        public Avalonia.Media.Imaging.Bitmap QRImage 
        { 
            get { return _qrImage; } 
            set { this.RaiseAndSetIfChanged(ref _qrImage, value); } 
        }

        /// <summary>
        /// The currently active identity.
        /// </summary>
        public SQRLIdentity Identity { get; }

        /// <summary>
        /// Creates a new instance and initializes.
        /// </summary>
        public ExportIdentityViewModel()
        {
            this.QRImage = null;
            this.Title = _loc.GetLocalizationValue("ExportIdentityWindowTitle");
            this.Identity = _identityManager.CurrentIdentity;

            var textualIdentityBytes = this.Identity.ToByteArray(includeHeader: true, minimumSize: true);
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(textualIdentityBytes, QRCodeGenerator.ECCLevel.H);
            QRCode qrCode = new QRCode(qrCodeData);
            
            var qrCodeBitmap = qrCode.GetGraphic(3, System.Drawing.Color.Black, System.Drawing.Color.White, true);
            
            using (var stream = new MemoryStream())
            {
                qrCodeBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                stream.Seek(0, SeekOrigin.Begin);
                this.QRImage = new Avalonia.Media.Imaging.Bitmap(stream);
            }
        }

        /// <summary>
        /// Saves the current identity as a file in the S4 storage format.
        /// </summary>
        public async void SaveToFile()
        {
            SaveFileDialog ofd = new SaveFileDialog();

            ofd.Title = _loc.GetLocalizationValue("SaveIdentityDialogTitle");
            ofd.InitialFileName = $"{(string.IsNullOrEmpty(this.Identity.IdentityName)?"Identity":this.Identity.IdentityName)}.sqrl";
            var file = await ofd.ShowAsync(_mainWindow);
            if(!string.IsNullOrEmpty(file))
            {
                this.Identity.WriteToFile(file);
                
                await new MessageBoxViewModel(_loc.GetLocalizationValue("IdentityExportedMessageBoxTitle"),
                    string.Format(_loc.GetLocalizationValue("IdentityExportedMessageBoxText"), file),
                    MessageBoxSize.Small, MessageBoxButtons.OK,MessageBoxIcons.OK)
                    .ShowDialog(this);
            }
        }

        /// <summary>
        /// Copies the textual version of the current identity to the clipboard.
        /// </summary>
        public async void CopyToClipboard()
        {
            var textualIdentityBytes = this.Identity.ToByteArray(includeHeader: true, minimumSize: true);

            string identity = SQRL.GenerateTextualIdentityBase56(textualIdentityBytes);
            await Application.Current.Clipboard.SetTextAsync(identity);
            
            await new MessageBoxViewModel(_loc.GetLocalizationValue("IdentityExportedMessageBoxTitle"),
                _loc.GetLocalizationValue("IdentityCopiedToClipboardMessageBoxText"),
                MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.OK)
                .ShowDialog(this);
        }

        /// <summary>
        /// Saves the current identity as a PDF file.
        /// </summary>
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
                await new MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"), ex.Message,
                    MessageBoxSize.Small, MessageBoxButtons.OK, MessageBoxIcons.OK)
                    .ShowDialog(this);
            }
        }

        /// <summary>
        /// Navigates back to the previous screen.
        /// </summary>
        public void Back()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
        }

        /// <summary>
        /// Displays the main screen.
        /// </summary>
        public void Done()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
        }
    }
}
