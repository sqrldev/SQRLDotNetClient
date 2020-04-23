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
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Avalonia.Controls;
using ReactiveUI;

namespace SQRLCommonUI.AvaloniaExtensions
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
        /// <summary>
        /// Magic string for marking the default localization.
        /// </summary>
        public static readonly string DEFAULT_LOC = "default";

        private string _resourceId { get; set; }
        private static Assembly _entryAssembly = Assembly.GetEntryAssembly();
        private static string _entryAssemblyName = Assembly.GetEntryAssembly().GetName().Name;
        private static Assembly _assembly = Assembly.GetExecutingAssembly();
        private static string _assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        private static JObject _localizationStrings { get; set; } = null;
        private static string _currentLocalization = DEFAULT_LOC;
        private static bool _initialized = false;

        /// <summary>
        /// Gets or sets the currently active localization using the
        /// format languagecode2-country/regioncode2 (e.g. "en-US").
        /// </summary>
        public static string CurrentLocalization
        {
            get { return _currentLocalization; }
            set
            {
                if (Localizations.ContainsKey(value))
                {
                    _currentLocalization = value;
                }
                else
                {
                    throw new ArgumentException("The specified localization is not yet supported!");
                }
            }
        }

        /// <summary>
        /// Gets or sets the <c>IValueConverter</c> to use.
        /// </summary>
        public IValueConverter Converter { get; set; }

        /// <summary>
        /// Provides a list of available localizations and their resources.
        /// </summary>
        public static Dictionary<string, LocalizationInfo> Localizations { get; } = new Dictionary<string, LocalizationInfo>();

        /// <summary>
        /// Creates a new <c>LocalizationExtension</c> instance.
        /// </summary>
        public LocalizationExtension()
        {

            if (!_initialized)
            {
                RegisterLocalizations();
                GetLocalization();
            }

            _initialized = true;
        }

        /// <summary>
        /// Registers all the available localizations.
        /// If a new translation language is added, it has to be added here.
        /// </summary>
        private void RegisterLocalizations()
        {
            string uriPath = $"{_assemblyName}.Assets.Localization.Flags.";

            // Register new localizations in this list!
            List<LocalizationInfo> localizations = new List<LocalizationInfo>()
            {
                new LocalizationInfo()
                {
                    CultureInfo = CultureInfo.CreateSpecificCulture("en-US"),
                    Image = new Bitmap(_assembly.GetManifestResourceStream(uriPath + "united_states_16.png"))
                },

                new LocalizationInfo()
                {
                    CultureInfo = CultureInfo.CreateSpecificCulture("de-DE"),
                    Image = new Bitmap(_assembly.GetManifestResourceStream(uriPath + "germany_16.png"))
                }

            };

            // Create the default localization and add id
            var defaultLoc = new LocalizationInfo()
            {
                CultureInfo = CultureInfo.CurrentCulture,
                Image = new Bitmap(_assembly.GetManifestResourceStream(uriPath + "default_16.png"))
            };

            Localizations.Add(DEFAULT_LOC, defaultLoc);

            // Now add the registered localizations
            foreach (var localization in localizations)
            {
                Localizations.Add(localization.CultureInfo.Name, localization);
            }
        }

        /// <summary>
        /// Creates a new <c>LocalizationExtension</c> instance and sets the 
        /// resource id to the given <paramref name="resourceID"/>.
        /// </summary>
        public LocalizationExtension(string resourceID) : this()
        {
            this._resourceId = resourceID;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return GetLocalizationValue(this._resourceId);
        }

        /// <summary>
        /// Returns the localized string value for the given <paramref name="resourceID"/>
        /// </summary>
        public string GetLocalizationValue(string resourceID)
        {
            var activeCulture = Localizations[_currentLocalization].CultureInfo;
            string localizedString = null;

            if (_localizationStrings.ContainsKey(activeCulture.Name))
            {
                try
                {
                    localizedString = ResolveFormatting(
                        _localizationStrings[activeCulture.Name].Children()[resourceID].First().ToString());
                }
                catch { }
            }

            if (localizedString == null)
            {
                try
                {
                    localizedString = ResolveFormatting(
                        _localizationStrings[DEFAULT_LOC].Children()[resourceID].First().ToString());
                }
                catch
                {
                    return "Missing translation: " + resourceID;
                }
            }

            if (Converter != null)
                localizedString = (string)Converter.Convert(localizedString, typeof(string), null, activeCulture);

            return localizedString;
        }

        /// <summary>
        /// Gets a list of menu item objects representing the different
        /// languages that are supported by the app.
        /// </summary>
        public List<MenuItem> GetLanguageMenuItems(ILocalizable target)
        {
            List<MenuItem> items = new List<MenuItem>();
            foreach (var locInfo in LocalizationExtension.Localizations)
            {
                object logo;
                string prefix = string.Empty;

                if (LocalizationExtension.CurrentLocalization == locInfo.Key)
                    logo = new CheckBox() { IsChecked = true, BorderThickness = new Thickness(0) };
                else
                    logo = new Image() { Source = locInfo.Value.Image };

                if (locInfo.Key == LocalizationExtension.DEFAULT_LOC)
                    prefix = GetLocalizationValue("DefaultLanguageMenuItemHeader") + " - ";

                MenuItem item = new MenuItem()
                {
                    Header = prefix + locInfo.Value.CultureInfo.DisplayName,
                    Command = ReactiveCommand.Create<string>(target.SelectLanguage),
                    CommandParameter = locInfo.Key,
                    Icon = logo
                };

                items.Add(item);
            }
            return items;
        }

        /// <summary>
        /// Reads the project's localization .json file into a <c>JObject</c>.
        /// </summary>
        private void GetLocalization()
        {
            _localizationStrings = (JObject)JsonConvert.DeserializeObject(new StreamReader(
                _entryAssembly.GetManifestResourceStream($"{_entryAssemblyName}.Assets.Localization.localization.json")).ReadToEnd());
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

    /// <summary>
    /// Holds information about a particular localization.
    /// </summary>
    public class LocalizationInfo
    {
        public CultureInfo CultureInfo;
        public Bitmap Image;
    }

    /// <summary>
    /// Defines an interface for localizable UI components.
    /// </summary>
    public interface ILocalizable
    {
        /// <summary>
        /// Changes the UI language in the current control to the
        /// language specified by <paramref name="language"/>.
        /// </summary>
        /// <param name="language"></param>
        public void SelectLanguage(string language);
    }
}
