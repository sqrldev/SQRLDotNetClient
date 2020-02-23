using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;

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

        public LocalizationExtension()
        {
            Assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var assy = Assembly.GetExecutingAssembly().GetName();
            Localization = (JObject)JsonConvert.DeserializeObject(new StreamReader(
                Assets.Open(new Uri($"resm:{assy.Name}.Assets.Localization.localization.json"))).ReadToEnd());
        }

        public LocalizationExtension(string resourceID)
        {
            this.ResourceID = resourceID;
            Assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var assy = Assembly.GetExecutingAssembly().GetName();
            Localization =(JObject) JsonConvert.DeserializeObject(new StreamReader(
                Assets.Open(new Uri($"resm:{assy.Name}.Assets.Localization.localization.json"))).ReadToEnd());
        }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return GetLocalizationValue(this.ResourceID);
        }

        public string GetLocalizationValue(string ResourceID)
        {
            var currentCulture = CultureInfo.CurrentCulture;
            if (Localization.ContainsKey(currentCulture.Name))
            {

                return Localization[currentCulture.Name].Children()[ResourceID].First().ToString();
            }
            else
                return Localization["default"].Children()[ResourceID].First().ToString();
        }

    }
}
