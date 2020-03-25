using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Data.Converters;

namespace SQRLDotNetClientUI.AvaloniaExtensions
{
    /// <summary>
    /// This extension allows you to have strings in different languages based on
    /// localization culture on the current machine
    /// This expects an Asset located in Assets/Localization/localization.json
    /// 
    /// The Format of the JSON in that file is as follows
    ///
    /*
    {
        "default":
        [
          {
            "SQRLTag":"Secure Quick Reliable Login"
          }
        ],
        "en-US":
        [
          {
            "SQRLTag":"Secure Quick Reliable Login"
          }
        ]
    } */
    /// The extension will use the default node if the current specific culture 
    /// isn't found in the file.
    /// </summary>
    public class LocalizationExtension : MarkupExtension
    {
        string ResourceID { get; set; }
        private IAssetLoader Assets { get; set; }

        private JObject Localization { get; set; }

        /// <summary>
        /// Gets or sets the <c>IValueConverter</c> to use.
        /// </summary>
        public IValueConverter Converter { get; set; }

        /// <summary>
        /// Creates a new <c>LocalizationExtension</c> instance.
        /// </summary>
        public LocalizationExtension()
        {
            GetLocalization();
        }

        /// <summary>
        /// Creates a new <c>LocalizationExtension</c> instance and sets the 
        /// resource id to the given <paramref name="resourceID"/>.
        /// </summary>
        public LocalizationExtension(string resourceID) : this()
        {
            this.ResourceID = resourceID;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return GetLocalizationValue(this.ResourceID);
        }

        /// <summary>
        /// Returns the localized string value for the given <paramref name="resourceID"/>
        /// </summary>
        /// <returns></returns>
        public string GetLocalizationValue(string resourceID)
        {
            var currentCulture = CultureInfo.CurrentCulture;
            string localizedString = null;

            if (Localization.ContainsKey(currentCulture.Name))
            {
                try
                {
                    localizedString = ResolveFormatting(
                        Localization[currentCulture.Name].Children()[resourceID].First().ToString());
                }
                catch { }
            }

            if (localizedString == null)
            {
                try
                {
                    localizedString = ResolveFormatting(
                        Localization["default"].Children()[resourceID].First().ToString());
                }
                catch
                {
                    return "Missing translation: " + resourceID;
                }
            }

            if (Converter != null)
                localizedString = (string)Converter.Convert(localizedString, typeof(string), null, currentCulture);

            return localizedString;
        }

        /// <summary>
        /// Reads the project's localization .json file into a <c>JObject</c>.
        /// </summary>
        private void GetLocalization()
        {
            Assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var assy = Assembly.GetExecutingAssembly().GetName();
            Localization = (JObject)JsonConvert.DeserializeObject(new StreamReader(
                Assets.Open(new Uri($"resm:{assy.Name}.Assets.Localization.localization.json"))).ReadToEnd());
        }

        /// <summary>
        /// Finds any escaped control sequences like "\n" in the given <paramref name="input"/>
        /// string and returns a string where any of such sequences are converted.
        /// </summary>
        /// <param name="input">The input string containing control sequences such as "\n".</param>
        private string ResolveFormatting(string input)
        {
            return input
                .Replace("\\r\\n", Environment.NewLine)
                .Replace("\\n", Environment.NewLine)
                .Replace("\\t", "\t");
        }
    }
}
