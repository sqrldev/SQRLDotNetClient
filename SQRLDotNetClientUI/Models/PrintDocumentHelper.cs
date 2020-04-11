using SQRLCommonUI.AvaloniaExtensions;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
using SkiaSharp;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace SQRLDotNetClientUI.Models
{
    /// <summary>
    /// A static helper class for creating and printing documents.
    /// </summary>
    public static class PrintDocumentHelper
    {
        private static readonly float PDF_MM_SCALE_FACTOR = 127f / 360f;
        private static readonly float HEADER_ICON_SIZE = 20f / PDF_MM_SCALE_FACTOR;
        private static readonly float FOOTER_ICON_SIZE = 10f / PDF_MM_SCALE_FACTOR;
        private static readonly float MARGIN_LEFT = 15f / PDF_MM_SCALE_FACTOR;
        private static readonly float MARGIN_TOP = 20f / PDF_MM_SCALE_FACTOR;
        private static readonly float PAGE_WIDTH = 210f / PDF_MM_SCALE_FACTOR;
        private static readonly float PAGE_HEIGHT = 297f / PDF_MM_SCALE_FACTOR;

        private static Assembly _assembly = Assembly.GetExecutingAssembly();
        private static string _assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        private static string _assetsPath = $"{_assemblyName}.Assets.";
        private static LocalizationExtension _loc = new LocalizationExtension();

        private static readonly SKTypeface _fontRegular = SKTypeface.FromStream(
            _assembly.GetManifestResourceStream(_assetsPath + "Fonts.Inconsolata-Regular.ttf"));
        private static readonly SKTypeface _fontBold = SKTypeface.FromStream(
            _assembly.GetManifestResourceStream(_assetsPath + "Fonts.Inconsolata-Bold.ttf"));

        private static string _rescueCode = "";
        private static string _identityName = "";

        /// <summary>
        /// Returns a pdf document displaying the given rescue code and
        /// some guidance for the user.
        /// </summary>
        /// <param name="rescueCode">The rescue code to print to the document.</param>
        /// <param name="identityName">The name of the identity. This is optional, just
        /// pass <c>null</c> if no identity name should be printed to the pdf file.</param>
        public static void CreateRescueCodeDocument(string rescueCode, string identityName = null)
        {
            var printerList = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
            //bool printerFound = printerList.Any(p => p == printerName);

            _rescueCode = rescueCode;
            _identityName = identityName;

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

            string filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                @"RescueCodeTest.pdf");

            using (var stream = new SKFileWStream(filePath))
            using (var document = SKDocument.CreatePdf(stream, metadata))
            {
                PrintRescueCodePage(document);
                document.Close();
            }

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        /// <summary>
        /// This does the actual drawing of the rescue code document.
        /// </summary>
        /// <param name="doc">The skia document to write to.</param>
        private static void PrintRescueCodePage(SKDocument doc)
        {
            float yPos = MARGIN_TOP + HEADER_ICON_SIZE + 80;
            string title = _loc.GetLocalizationValue("RescueCodeDocumentTitle");
            string warning = _loc.GetLocalizationValue("RescueCodeDocumentDiscardWarning");
            string text = _loc.GetLocalizationValue("RescueCodeDocumentText");
            string identityLabel = _loc.GetLocalizationValue("IdentityNameLabel");
            string rescueCodeLabel = _loc.GetLocalizationValue("RescueCodeLabel");

            using (var canvas = doc.BeginPage(PAGE_WIDTH, PAGE_HEIGHT))
            {
                DrawHeader(canvas);
                yPos += 10 + DrawTextBlock(canvas, title, yPos, _fontBold, 25, SKColors.Black);
                yPos += 20 + DrawTextBlock(canvas, warning, yPos, _fontBold, 20, SKColors.Red);
                yPos += 30 + DrawTextBlock(canvas, text, yPos, _fontRegular, 12, SKColors.DarkGray, SKTextAlign.Left, 1.3f);
                yPos += 10 + DrawTextBlock(canvas, identityLabel, yPos, _fontRegular, 12, SKColors.DarkGray);
                yPos += 20 + DrawTextBlock(canvas, _identityName, yPos, _fontBold, 16, SKColors.Black);
                yPos += 10 + DrawTextBlock(canvas, rescueCodeLabel, yPos, _fontRegular, 12, SKColors.DarkGray);
                yPos += 00 + DrawTextBlock(canvas, _rescueCode, yPos, _fontBold, 24, SKColors.Black);
                DrawFooter(canvas);

                doc.EndPage();
            }
        }

        /// <summary>
        /// Draws the header logo and text onto the document.
        /// </summary>
        /// <param name="canvas">The skia canvas to draw to.</param>
        private static void DrawHeader(SKCanvas canvas)
        {
            float imgX = PAGE_WIDTH / 2 - HEADER_ICON_SIZE / 2;
            float imgY = MARGIN_TOP;

            var logoStream = _assembly.GetManifestResourceStream(_assetsPath + "SQRL_icon_normal_256.png");
            logoStream.Seek(0, SeekOrigin.Begin);

            canvas.DrawBitmap(SKBitmap.Decode(logoStream), 
                new SKRect(imgX, imgY, imgX + HEADER_ICON_SIZE, imgY + HEADER_ICON_SIZE));

            DrawTextBlock(canvas, 
                _loc.GetLocalizationValue("SQRLTag"), 
                imgY + HEADER_ICON_SIZE + 20, 
                _fontBold, 12, SKColors.Black);
        }

        /// <summary>
        /// Draws the footer logo and text onto the document.
        /// </summary>
        /// <param name="canvas">The skia canvas to draw to.</param>
        private static void DrawFooter(SKCanvas canvas)
        {
            float imgX = PAGE_WIDTH / 2 - FOOTER_ICON_SIZE / 2;
            float imgY = PAGE_HEIGHT - MARGIN_TOP - FOOTER_ICON_SIZE;

            var logoStream = _assembly.GetManifestResourceStream(_assetsPath + "SQRL_icon_normal_256.png");
            logoStream.Seek(0, SeekOrigin.Begin);

            canvas.DrawBitmap(SKBitmap.Decode(logoStream),
                new SKRect(imgX, imgY, imgX + FOOTER_ICON_SIZE, imgY + FOOTER_ICON_SIZE));

            string footerText = string.Format(
                _loc.GetLocalizationValue("PrintDocumentFooterMessage"),
                _assemblyName + " v " + _assembly.GetName().Version,
                DateTime.Now.ToLongDateString());

            DrawTextBlock(canvas, footerText,
                imgY + FOOTER_ICON_SIZE + 20,
                _fontRegular, 8, SKColors.DarkGray);
        }

        /// <summary>
        /// Draws a block of text onto the document and returns the height of that block.
        /// </summary>
        /// <param name="canvas">The skia canvas to draw onto.</param>
        /// <param name="text">The text to be printed onto the document.</param>
        /// <param name="y">The y position of the text block.</param>
        /// <param name="typeface">The font to be used for drawing the text.</param>
        /// <param name="textSize">The size of the text.</param>
        /// <param name="color">The color to be used for drawing the text.</param>
        /// <param name="textAlign">The alignment for the text to be drawn.</param>
        /// <param name="lineHeightFactor">A value indicating the line heigt. The default line heigt factor is 1.</param>
        /// <returns>Returns the height of the text block which was drawn to the document.</returns>
        private static float DrawTextBlock(SKCanvas canvas, string text, float y, SKTypeface typeface, float textSize,
            SKColor color, SKTextAlign textAlign = SKTextAlign.Center, float lineHeightFactor = 1.0f)
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

                int lines = 1;
                StringBuilder line = new StringBuilder();
                var tokens = Tokenize(text);

                for (int i=0; i < tokens.Count; i++)
                {
                    StartLine:
                    var token = tokens[i];
                    float totalLineWidth = 0;
                    
                    if (token != null)
                    {
                        totalLineWidth = line.Length > 0 ?
                        paint.MeasureText(line + " " + token) :
                        paint.MeasureText(token);
                    }

                    if (totalLineWidth >= blockWidth || token == null)
                    {
                        canvas.DrawText(line.ToString(), xPos, y, paint);
                        line.Clear();
                        lines++;
                        y += lineHeight;
                        if (token != null) goto StartLine;
                    }
                    else
                    {
                        if (line.Length != 0) line.Append(" ");
                        line.Append(token);

                        if (i == tokens.Count - 1) // Last word
                        {
                            canvas.DrawText(line.ToString(), xPos, y, paint);
                            line.Clear();
                            lines++;
                            y += lineHeight;
                        }
                    }
                }                

                return lines * lineHeight;
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
