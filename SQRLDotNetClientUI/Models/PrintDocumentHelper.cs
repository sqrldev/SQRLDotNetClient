using SQRLCommonUI.AvaloniaExtensions;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;

namespace SQRLDotNetClientUI.Models
{
    /// <summary>
    /// A static helper class for creating and printing documents.
    /// </summary>
    public static class PrintDocumentHelper
    {
        private static readonly int HEADER_ICON_SIZE = 64;
        private static readonly int FOOTER_ICON_SIZE = 32;
        private static readonly int MARGIN_LEFT = 32;
        private static readonly int MARGIN_TOP = 64;

        private static readonly Font _headlineFont = new Font("Consolas", 20, FontStyle.Bold);
        private static readonly Font _warningFont = new Font("Consolas", 16, FontStyle.Bold);
        private static readonly Font _textFont = new Font("Consolas", 12);
        private static readonly Font _footerFont = new Font("Consolas", 8);

        private static Assembly _assembly = Assembly.GetExecutingAssembly();
        private static string _assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        private static string _assetsPath = $"{_assemblyName}.Assets.";
        private static Image _sqrlLogo = new Bitmap(_assembly.GetManifestResourceStream(_assetsPath + "SQRL_icon_normal_256.png"));
        private static LocalizationExtension _loc = new LocalizationExtension();

        private static string _rescueCode = "";
        private static string _identityName = "";

        /// <summary>
        /// Returns a pdf document displaying the given rescue code and
        /// some guidance for the user.
        /// </summary>
        /// <param name="rescueCode">The rescue code to print to the document.</param>
        /// <param name="identityName">The name of the identity. This is optional, just
        /// pass <c>null</c> if no identity name should be printed to the pdf file.</param>
        /// <returns></returns>
        public static PrintDocument CreateRescueCodeDocument(string rescueCode, string identityName = null)
        {
            var printerList = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
            //bool printerFound = printerList.Any(p => p == printerName);

            _rescueCode = rescueCode;
            _identityName = identityName;

            PrintDocument pd = new PrintDocument();
            pd.PrintPage += new PrintPageEventHandler(PrintRescueCodePage);
            pd.DocumentName = "SQRL Rescue Code Document";
            pd.PrinterSettings.PrinterName = "Microsoft Print to PDF";
            pd.PrinterSettings.PrintFileName = @"C:\Temp\test.pdf";
            pd.PrinterSettings.PrintToFile = true;
            
            pd.Print();
            return pd;
        }

        /// <summary>
        /// This is the print callback that does the actual drawing of the 
        /// rescue code document.
        /// </summary>
        /// <param name="sender">The sender of the print event.</param>
        /// <param name="ev">The PrintPageEventArgs representing the document to be printed.</param>
        private static void PrintRescueCodePage(object sender, PrintPageEventArgs ev)
        {
            float yPos = MARGIN_TOP + HEADER_ICON_SIZE + 50;

            DrawHeader(ev);
            yPos += 30 + DrawTextBlock(ev, _loc.GetLocalizationValue("RescueCodeDocumentTitle"), yPos, _headlineFont, Brushes.Black);
            yPos += 30 + DrawTextBlock(ev, _loc.GetLocalizationValue("RescueCodeDocumentDiscardWarning"), yPos, _warningFont, Brushes.DarkRed);
            yPos += 50 + DrawTextBlock(ev, _loc.GetLocalizationValue("RescueCodeDocumentText"), yPos);
            yPos += 15 + DrawTextBlock(ev, _loc.GetLocalizationValue("IdentityNameLabel"), yPos);
            yPos += 50 + DrawTextBlock(ev, _identityName, yPos, _warningFont, Brushes.Black);
            yPos += 15 + DrawTextBlock(ev, _loc.GetLocalizationValue("RescueCodeLabel"), yPos);
            yPos += 50 + DrawTextBlock(ev, _rescueCode, yPos, _warningFont, Brushes.Black);
            DrawFooter(ev);

            ev.HasMorePages = false;
        }

        /// <summary>
        /// Draws the header logo onto the document.
        /// </summary>
        /// <param name="ev">The PrintPageEventArgs representing the document to be printed.</param>
        private static void DrawHeader(PrintPageEventArgs ev)
        {
            ev.Graphics.DrawImage(
                _sqrlLogo,
                ev.PageBounds.Width / 2 - HEADER_ICON_SIZE / 2,
                MARGIN_TOP,
                HEADER_ICON_SIZE,
                HEADER_ICON_SIZE
            );
        }

        /// <summary>
        /// Draws the footer logo and text onto the document.
        /// </summary>
        /// <param name="ev">The PrintPageEventArgs representing the document to be printed.</param>
        private static void DrawFooter(PrintPageEventArgs ev)
        {
            ev.Graphics.DrawImage(
                _sqrlLogo,
                ev.PageBounds.Width / 2 - FOOTER_ICON_SIZE / 2,
                ev.PageBounds.Height - FOOTER_ICON_SIZE - MARGIN_TOP - 10,
                FOOTER_ICON_SIZE,
                FOOTER_ICON_SIZE
            );

            float yPos = ev.PageBounds.Height - MARGIN_TOP;
            string footerText = string.Format(
                _loc.GetLocalizationValue("PrintDocumentFooterMessage"),
                _assemblyName + " v " + _assembly.GetName().Version,
                DateTime.Now.ToLongDateString());

            DrawTextBlock(ev, footerText, yPos, _footerFont);
        }

        /// <summary>
        /// Draws a block of text onto the document and returns the height of that block.
        /// </summary>
        /// <param name="ev">The PrintPageEventArgs representing the document to be printed.</param>
        /// <param name="text">The text to be printed onto the document.</param>
        /// <param name="y">The y position of the text block.</param>
        /// <param name="font">The font to be used for drawing the text.</param>
        /// <param name="brush">The brush to be used for drawing the text.</param>
        /// <param name="format">The string format to be used for drawing the text.</param>
        /// <returns>Returns the height of the text block which was drawn to the document.</returns>
        private static float DrawTextBlock(PrintPageEventArgs ev, string text, float y, Font font = null, 
            Brush brush = null, StringFormat format = null)
        {
            if (font == null) font = _textFont;
            if (brush == null) brush = Brushes.Gray;
            if (format == null)
            {
                format = new StringFormat();
                format.LineAlignment = StringAlignment.Center;
                format.Alignment = StringAlignment.Center;
            }

            float textBoxWidth = ev.PageBounds.Width - (MARGIN_LEFT * 2);

            // Measure text
            var size = ev.Graphics.MeasureString(
                text,
                font,
                new SizeF(textBoxWidth, 500),
                format);

            // Draw text
            ev.Graphics.DrawString(
                text,
                font,
                brush,
                new RectangleF(MARGIN_LEFT, y, textBoxWidth, size.Height),
                format);

            return size.Height;
        }
    }
}
