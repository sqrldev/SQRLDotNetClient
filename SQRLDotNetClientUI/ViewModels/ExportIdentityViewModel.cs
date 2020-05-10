using Avalonia;
using Avalonia.Controls;
using QRCoder;
using ReactiveUI;
using Serilog;
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
        private bool _exportWithPassword = true;
        private bool _showQrCode = false;
        private bool _showBackButton = true;

        /// <summary>
        /// Gets a list of block types to export depending on the export
        /// option chosen by the user (pwd + rc code or rc only).
        /// </summary>
        private List<ushort> _blocksToExport
        {
            get
            {
                return this.ExportWithPassword ?
                    new List<ushort>() { 1, 2, 3 } :
                    new List<ushort>() { 2, 3 };
            }
        }

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
        /// Gets or sets a value indicating whether the exported identity
        /// can be decrypted with either the password and the rescue code
        /// or with the rescue code only.
        /// </summary>
        public bool ExportWithPassword 
        {
            get { return _exportWithPassword; }
            set { this.RaiseAndSetIfChanged(ref _exportWithPassword, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to display the
        /// identity qr-code in the UI.
        /// </summary>
        public bool ShowQrCode
        {
            get { return _showQrCode; }
            set { this.RaiseAndSetIfChanged(ref _showQrCode, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to display the
        /// "back" button in the UI.
        /// </summary>
        public bool ShowBackButton
        {
            get { return _showBackButton; }
            set { this.RaiseAndSetIfChanged(ref _showBackButton, value); }
        }

        /// <summary>
        /// Creates a new instance and initializes.
        /// </summary>
        public ExportIdentityViewModel(bool showBackButton = false)
        {
            this.QRImage = null;
            this.Title = _loc.GetLocalizationValue("ExportIdentityWindowTitle");
            this.ShowBackButton = showBackButton;
            this.Identity = _identityManager.CurrentIdentity;

            this.WhenAnyValue(x => x.ExportWithPassword)
                .Subscribe(x =>
                {
                    UpdateQrCode();
                });

            UpdateQrCode();
        }

        /// <summary>
        /// Updates the qr-code image.
        /// </summary>
        private async void UpdateQrCode()
        {
            // If ExportIdentityView is not yet fully loaded, we need to drop out
            // because if a message box gets shown in this state, it will mess up 
            // our "content/previous content" system.
            if (((MainWindowViewModel)_mainWindow.DataContext).Content.GetType() != typeof(ExportIdentityViewModel))
                return;

            var identityBytes = this.Identity.ToByteArray(includeHeader: true, _blocksToExport);

            try
            {
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(identityBytes, QRCodeGenerator.ECCLevel.M);
                QRCode qrCode = new QRCode(qrCodeData);

                var qrCodeBitmap = qrCode.GetGraphic(3, System.Drawing.Color.Black, System.Drawing.Color.White, true);

                using (var stream = new MemoryStream())
                {
                    qrCodeBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                    stream.Seek(0, SeekOrigin.Begin);
                    this.QRImage = new Avalonia.Media.Imaging.Bitmap(stream);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error creating QR code: {ex.Message}");

                await new MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                    _loc.GetLocalizationValue("MissingLibGdiPlusErrorMessage"),
                    MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                    .ShowDialog(this);
            }
        }

        /// <summary>
        /// Saves the current identity as a file in the S4 storage format.
        /// </summary>
        public async void SaveToFile()
        {
            SaveFileDialog sfd = new SaveFileDialog();

            var fileExtension = this.ExportWithPassword ? "sqrl" : "sqrc";
            sfd.Title = _loc.GetLocalizationValue("SaveIdentityDialogTitle");
            sfd.InitialFileName = $"{(string.IsNullOrEmpty(this.Identity.IdentityName)?"Identity":this.Identity.IdentityName)}.{fileExtension}";
            sfd.DefaultExtension = fileExtension;
            var file = await sfd.ShowAsync(_mainWindow);
            if(!string.IsNullOrEmpty(file))
            {
                this.Identity.WriteToFile(file, skipBlockType1: !this.ExportWithPassword);
                
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
            var textualIdentityBytes = this.Identity.ToByteArray(includeHeader: true, _blocksToExport);

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
                PdfHelper.CreateIdentityDocument(file, this.Identity, _blocksToExport); 
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = file,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (TypeInitializationException ex)
            {
                Log.Error($"{ex.GetType().ToString()} was thrown while creating an identity pdf: {ex.Message}");

                await new MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                    _loc.GetLocalizationValue("MissingLibGdiPlusErrorMessage"),
                    MessageBoxSize.Small, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                    .ShowDialog(this);
            }
            catch (Exception ex) 
            {
                Log.Error($"{ex.GetType().ToString()} was thrown while creating an identity pdf: {ex.Message}");

                await new MessageBoxViewModel(_loc.GetLocalizationValue("ErrorTitleGeneric"), ex.Message,
                    MessageBoxSize.Small, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                    .ShowDialog(this);
            }
        }

        /// <summary>
        /// Toggles the visibility of the qr code UI.
        /// </summary>
        /// <param name="visible">Set to <c>true</c> to show the qr code, 
        /// or <c>false</c> to hide it</param>
        public void ToggleQrCode(bool visible)
        {
            UpdateQrCode();
            this.ShowQrCode = visible;
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
