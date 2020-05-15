using SQRLCommonUI.AvaloniaExtensions;
using System;
using System.Linq;
using System.Reflection;
using SkiaSharp;
using System.IO;
using System.Text;
using System.Collections.Generic;
using SQRLUtilsLib;
using QRCoder;

namespace SQRLDotNetClientUI.Models
{
    /// <summary>
    /// A static helper class for creating PDF documents.
    /// </summary>
    public static class PdfHelper
    {
        private static readonly float PDF_MM_SCALE_FACTOR = 127f / 360f;
        private static readonly float HEADER_ICON_SIZE = 20f / PDF_MM_SCALE_FACTOR;
        private static readonly float FOOTER_ICON_SIZE = 10f / PDF_MM_SCALE_FACTOR;
        private static readonly float QRCODE_MAX_SIZE = 60f / PDF_MM_SCALE_FACTOR;
        private static readonly float MARGIN_LEFT = 15f / PDF_MM_SCALE_FACTOR;
        private static readonly float MARGIN_TOP = 20f / PDF_MM_SCALE_FACTOR;
        private static readonly float PAGE_WIDTH = 210f / PDF_MM_SCALE_FACTOR;
        private static readonly float PAGE_HEIGHT = 297f / PDF_MM_SCALE_FACTOR;

        private static Assembly _assembly = Assembly.GetExecutingAssembly();
        private static string _assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        private static string _assetsPath = $"{_assemblyName}.Assets.";
        private static LocalizationExtension _loc = new LocalizationExtension();
        private static SKDocument _document = null;
        private static SKCanvas _canvas = null;
        private static int _pageNr = 0;
        private static float _yPos = 0.0f;

        /* 
         * Loading custom fonts produces an empty page on linux, maybe this is
         * a SkiaSharp bug that will be fixed in the future.
         * 
         * 
        private static readonly SKTypeface _fontRegular = SKTypeface.FromStream(
            _assembly.GetManifestResourceStream(_assetsPath + "Fonts.UbuntuMono-Regular.ttf"));
        private static readonly SKTypeface _fontBold = SKTypeface.FromStream(
            _assembly.GetManifestResourceStream(_assetsPath + "Fonts.UbuntuMono-Bold.ttf")); 
        */

        private static readonly SKTypeface _fontMonoRegular = SKTypeface.FromFamilyName("Courier New");
        private static readonly SKTypeface _fontMonoBold = SKTypeface.FromFamilyName("Courier New",
            SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

        private static readonly SKTypeface _fontRegular = SKTypeface.FromFamilyName("Arial");
        private static readonly SKTypeface _fontBold = SKTypeface.FromFamilyName("Arial",
            SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

        /// <summary>
        /// Creates a PDF document prominently displaying the given rescue code and 
        /// providing some guidance for the user.
        /// </summary>
        /// <param name="fileName">The full file name (including the path) for the document.</param>
        /// <param name="rescueCode">The rescue code to be printed to the document.</param>
        /// <param name="identityName">The name of the identity. This is optional, just
        /// pass <c>null</c> if no identity name should be printed to the pdf file.</param>
        public static void CreateRescueCodeDocument(string fileName, string rescueCode, string identityName = null)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(rescueCode))
            {
                throw new ArgumentException(string.Format("{0} and {1} must be specified!",
                    nameof(fileName), nameof(rescueCode)));
            }

            identityName = identityName != null ? identityName : "";

            _pageNr = 0;
            string title = _loc.GetLocalizationValue("RescueCodeDocumentTitle");
            string warning = _loc.GetLocalizationValue("RescueCodeDocumentDiscardWarning");
            string text = _loc.GetLocalizationValue("RescueCodeDocumentText");
            string identityLabel = _loc.GetLocalizationValue("IdentityNameLabel");
            string rescueCodeLabel = _loc.GetLocalizationValue("RescueCodeLabel");

            var metadata = new SKDocumentPdfMetadata
            {
                Author = _assemblyName,
                Creation = DateTime.Now,
                Creator = _assemblyName,
                Keywords = "SQRL,Rescue code",
                Modified = DateTime.Now,
                Producer = "SkiaSharp",
                Subject = "SQRL Rescue Code Document",
                Title = "SQRL Rescue Code Document"
            };

            using (var stream = new SKFileWStream(fileName))
            using (_document = SKDocument.CreatePdf(stream, metadata))
            {
                StartNextPage();
                DrawHeader();

                _yPos = MARGIN_TOP + HEADER_ICON_SIZE + 80;
                DrawTextBlock(title, _fontBold, 25, SKColors.Black, 15f);
                DrawTextBlock(warning, _fontBold, 20, SKColors.Red, 20f);
                DrawTextBlock(text, _fontRegular, 12, SKColors.DarkGray, 30f, SKTextAlign.Left, 1.3f);

                if (!string.IsNullOrEmpty(identityName))
                {
                    DrawTextBlock(identityLabel, _fontRegular, 12, SKColors.DarkGray, 15f);
                    DrawTextBlock(identityName, _fontBold, 16, SKColors.Black);
                }
                DrawTextBlock(rescueCodeLabel, _fontRegular, 12, SKColors.DarkGray, 25f);
                DrawTextBlock(rescueCode, _fontBold, 24, SKColors.Black);

                EndPage();
                _document.Close();
            }
        }

        /// <summary>
        /// Creates a PDF document displaying the given identity as QR code and text and 
        /// providing some guidance for the user.
        /// </summary>
        /// <param name="fileName">The full file name (including the path) for the document.</param>
        /// <param name="identity">The identity for which to create the document.</param>
        /// <param name="blockTypes">Spciefies a list of block types to include.</param>
        public static void CreateIdentityDocument(string fileName, SQRLIdentity identity, List<ushort> blockTypes)
        {
            if (string.IsNullOrEmpty(fileName) || identity == null)
            {
                throw new ArgumentException(string.Format("{0} and {1} must be specified and valid!", 
                    nameof(fileName), nameof(identity)));
            }

            _pageNr = 0;
            string title = "\"" + identity.IdentityName + "\" " + _loc.GetLocalizationValue("FileDialogFilterName");
            string identityEncryptionMessage = blockTypes.Contains(1) ?
                _loc.GetLocalizationValue("IdentityDocEncMsgPassword") :
                _loc.GetLocalizationValue("IdentityDocEncMsgRC");
            string textualIdentityMessage = _loc.GetLocalizationValue("IdentityDocumentTextualIdentityMessage");
            string guidanceMessage = _loc.GetLocalizationValue("IdentityDocumentGuidanceMessage");
            var identityBytes = identity.ToByteArray(includeHeader: true, blockTypes);
            string textualIdentity = SQRL.GenerateTextualIdentityBase56(identityBytes);

            var metadata = new SKDocumentPdfMetadata
            {
                Author = _assemblyName,
                Creation = DateTime.Now,
                Creator = _assemblyName,
                Keywords = "SQRL,Identity",
                Modified = DateTime.Now,
                Producer = "SkiaSharp",
                Subject = "SQRL Identity Document",
                Title = "SQRL Identity Document",
                EncodingQuality = 300,
                RasterDpi = 300
            };

            using (SKBitmap qrCode = CreateQRCode(identityBytes))
            using (var stream = new SKFileWStream(fileName))
            using (_document = SKDocument.CreatePdf(stream, metadata))
            {
                StartNextPage();

                float qrCodeWidth = (qrCode.Width <= QRCODE_MAX_SIZE) ? qrCode.Width : QRCODE_MAX_SIZE;
                float qrCodeHeight = (qrCode.Height <= QRCODE_MAX_SIZE) ? qrCode.Height : QRCODE_MAX_SIZE;
                float qrCodeXPos = PAGE_WIDTH / 2 - qrCodeWidth / 2;

                DrawTextBlock(title, _fontBold, 23, SKColors.Black, 10f);
                DrawTextBlock(identityEncryptionMessage, _fontRegular, 12, SKColors.DarkGray, -5f, SKTextAlign.Left, 1.3f);
                DrawBitmap(qrCode, new SKRect(qrCodeXPos, _yPos, qrCodeXPos + qrCodeWidth, _yPos + qrCodeHeight), 15f);
                DrawTextBlock(textualIdentityMessage, _fontRegular, 12, SKColors.DarkGray, 10f, SKTextAlign.Left, 1.3f);
                DrawTextBlock(textualIdentity, _fontMonoBold, 12, SKColors.Black, 15f, SKTextAlign.Center, 1.3f); ;
                DrawTextBlock(guidanceMessage, _fontRegular, 12, SKColors.DarkGray, 0f, SKTextAlign.Left, 1.3f);

                EndPage();
                _document.Close();
            }
        }

        /// <summary>
        /// Ends the current page if it exists and starts a new page.
        /// </summary>
        private static void StartNextPage()
        {
            EndPage();
            _canvas = _document.BeginPage(PAGE_WIDTH, PAGE_HEIGHT);
            _yPos = MARGIN_TOP;
            _pageNr++;
        }

        /// <summary>
        /// Draws the footer and ends the current page.
        /// </summary>
        private static void EndPage()
        {
            if (_document == null) return;

            if (_canvas != null)
            {
                DrawFooter();

                _canvas.Flush();
                _document.EndPage();
                _canvas.Dispose();
                _canvas = null;
            }
        }

        /// <summary>
        /// Creates and returns a QR code bitmap representing the given identityBytes.
        /// </summary>
        /// <param name="identityBytes">The raw identity bytes.</param>
        private static SKBitmap CreateQRCode(byte[] identityBytes)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(identityBytes, QRCodeGenerator.ECCLevel.M);
            QRCode qrCode = new QRCode(qrCodeData);
            var bitmap = qrCode.GetGraphic(3, System.Drawing.Color.Black, System.Drawing.Color.White, true);
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                stream.Seek(0, SeekOrigin.Begin);
                return SKBitmap.Decode(stream);
            }
        }

        /// <summary>
        /// Draws the header logo and text onto the document.
        /// </summary>
        /// <param name="canvas">The skia canvas to draw to.</param>
        private static void DrawHeader()
        {
            float imgX = PAGE_WIDTH / 2 - HEADER_ICON_SIZE / 2;
            float imgY = MARGIN_TOP;

            var logoStream = _assembly.GetManifestResourceStream(_assetsPath + "SQRL_icon_normal_256.png");
            logoStream.Seek(0, SeekOrigin.Begin);

            _canvas.DrawBitmap(SKBitmap.Decode(logoStream), 
                new SKRect(imgX, imgY, imgX + HEADER_ICON_SIZE, imgY + HEADER_ICON_SIZE));

            float yBackup = _yPos; // Backup current y position
            _yPos = imgY + HEADER_ICON_SIZE + 20;
            DrawTextBlock(_loc.GetLocalizationValue("SQRLTag"), _fontBold, 12, SKColors.Black);
            _yPos = yBackup; // Restore y position
        }

        /// <summary>
        /// Draws the footer logo and text onto the document.
        /// </summary>
        /// <param name="canvas">The skia canvas to draw to.</param>
        private static void DrawFooter()
        {
            float imgX = PAGE_WIDTH / 2 - FOOTER_ICON_SIZE / 2;
            float imgY = PAGE_HEIGHT - MARGIN_TOP - FOOTER_ICON_SIZE;

            var logoStream = _assembly.GetManifestResourceStream(_assetsPath + "SQRL_icon_normal_256.png");
            logoStream.Seek(0, SeekOrigin.Begin);

            _canvas.DrawBitmap(SKBitmap.Decode(logoStream),
                new SKRect(imgX, imgY, imgX + FOOTER_ICON_SIZE, imgY + FOOTER_ICON_SIZE));

            string footerText = string.Format(
                _loc.GetLocalizationValue("PrintDocumentFooterMessage"),
                _assemblyName + " v " + _assembly.GetName().Version, DateTime.Now.ToLongDateString());

            float yBackup = _yPos; // Backup current y position
            _yPos = imgY + FOOTER_ICON_SIZE + 20;
            DrawTextBlock(footerText, _fontRegular, 8, SKColors.DarkGray, 3f,
                SKTextAlign.Center, 1f, true);
            DrawTextBlock("Page " + _pageNr, _fontRegular, 6, SKColors.DarkGray, 0f,
                SKTextAlign.Right, 1f, true);
            _yPos = yBackup; // Restore y position
        }

        /// <summary>
        /// Draws a bitmap to the document and returns the height of the drawing,
        /// excluding the <paramref name="paddingBottom"/>.
        /// </summary>
        /// <param name="bitmap">The bitmap to draw onto the document.</param>
        /// <param name="rect">The position and size for the drawing operation.</param>
        /// <param name="paddingBottom">Adds additional vertical "whitespace" below the bitmap.</param>
        private static float DrawBitmap(SKBitmap bitmap, SKRect rect, float paddingBottom = 15f)
        {
            CheckAndTriggerPageBreak(rect.Height);

            _canvas.DrawBitmap(bitmap, rect);
            _yPos += rect.Height + paddingBottom;

            return rect.Height;
        }

        /// <summary>
        /// Draws a block of text onto the document and returns the height of that block,
        /// excluding the secified <paramref name="paddingBottom"/>.
        /// </summary>
        /// <param name="text">The text to be printed onto the document.</param>
        /// <param name="typeface">The font to be used for drawing the text.</param>
        /// <param name="textSize">The size of the text.</param>
        /// <param name="color">The color to be used for drawing the text.</param>
        /// <param name="paddingBottom">Adds additional vertical "whitespace" below the textblock.</param>
        /// <param name="textAlign">The alignment for the text to be drawn.</param>
        /// <param name="lineHeightFactor">A value indicating the line heigt. The default line heigt factor is 1.</param>
        /// <param name="noPageBreak">If set to <c>treue</c>, drawing the text will never cause a page break.</param>
        /// <returns>Returns the height of the text block which was drawn to the document, excluding <paramref name="paddingBottom"/>.</returns>
        private static float DrawTextBlock(string text, SKTypeface typeface, float textSize, SKColor color, float paddingBottom = 15f,
            SKTextAlign textAlign = SKTextAlign.Center, float lineHeightFactor = 1.0f, bool noPageBreak = false)
        {
            List<string> lineBreakSequences = new List<string>() {"\r", "\n", "\r\n" };

            using (var paint = new SKPaint())
            {
                paint.Typeface = typeface;
                paint.TextSize = textSize;
                paint.IsAntialias = true;
                paint.Color = color;
                paint.TextAlign = textAlign;

                float blockWidth = PAGE_WIDTH - (MARGIN_LEFT * 2);

                // Define x position according to chosen text alignment
                float xPos = 0f;
                switch (textAlign)
                {
                    case SKTextAlign.Left:
                        xPos = MARGIN_LEFT;
                        break;

                    case SKTextAlign.Center:
                        xPos = PAGE_WIDTH / 2;
                        break;

                    case SKTextAlign.Right:
                        xPos = PAGE_WIDTH - MARGIN_LEFT;
                        break;
                }

                // Measure the height of one line of text
                SKRect bounds = new SKRect();
                paint.MeasureText(text, ref bounds);
                float lineHeight = bounds.Height * lineHeightFactor;

                // Now loop through all the tokens and build the 
                // text block, adding new lines when we're either
                // out of space or when a newline if forced in the 
                // token list.
                int lines = 1;
                StringBuilder line = new StringBuilder();
                var tokens = Tokenize(text);

                for (int i=0; i < tokens.Count; i++)
                {
                    StartLine:
                    var token = tokens[i];
                    float totalLineWidth = 0;
                    
                    if (token != null && token != "")
                    {
                        totalLineWidth = line.Length > 0 ?
                        paint.MeasureText(line + " " + token) :
                        paint.MeasureText(token);
                    }

                    if (totalLineWidth >= blockWidth || token == null)
                    {
                        if (!noPageBreak) CheckAndTriggerPageBreak(lineHeight);
                        _canvas.DrawText(line.ToString(), xPos, _yPos, paint);
                        line.Clear();
                        lines++;
                        _yPos += lineHeight;
                        if (token != null) goto StartLine;
                    }
                    else
                    {
                        if (line.Length != 0) line.Append(" ");
                        line.Append(token);

                        if (i == tokens.Count - 1) // Last word
                        {
                            if (!noPageBreak) CheckAndTriggerPageBreak(lineHeight);
                            _canvas.DrawText(line.ToString(), xPos, _yPos, paint);
                            line.Clear();
                            lines++;
                            _yPos += lineHeight;
                        }
                    }
                }

                _yPos += paddingBottom;

                // Return the total height of the text block
                return lines * lineHeight;
            }
        }

        /// <summary>
        /// Checks if the current Y position plus the given <paramref name="height"/> 
        /// exceeds the maximum Y position for the page, and if it does, triggers a
        /// page break.
        /// </summary>
        /// <param name="height">The heigt of the object to check.</param>
        private static void CheckAndTriggerPageBreak(float height)
        {
            float maxY =  PAGE_HEIGHT - MARGIN_TOP - 30;

            if (_yPos + height > maxY)
            {
                StartNextPage();
            }
        }

        /// <summary>
        /// Splits up text into tokens (mostly words) and adds null entries
        /// to signal line breaks.
        /// </summary>
        /// <param name="text">The text to be tokenized.</param>
        /// <returns>Returns a list of tokens, with null values representing line breaks.</returns>
        private static List<string> Tokenize(string text)
        {
            char[] separators = new char[] { ' ', '\r', '\n' };
            List<string> result = new List<string>();
            StringBuilder word = new StringBuilder();

            for (int i=0; i<text.Length; i++)
            {
                char c = text[i];

                if (separators.Contains(c))
                {
                    result.Add(word.ToString());
                    word.Clear();

                    if (c == '\r' || c == '\n')
                    {
                        // Add null to indicate a line break
                        result.Add(null);

                        // If the \r is followed by \n, then skip the \n
                        if (c == '\r' && i != text.Length-1)
                        {
                            if (text[i + 1] == '\n') i += 1;
                        }
                    }
                }
                else
                {
                    word.Append(c);

                    // If we're at the last character, we need to add
                    // what we've got to the result as well.
                    if (i == text.Length - 1)
                    {
                        result.Add(word.ToString());
                    }
                }
            }

            return result;
        }
    }
}
