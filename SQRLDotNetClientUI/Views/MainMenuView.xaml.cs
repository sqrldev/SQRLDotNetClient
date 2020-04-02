using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SQRLDotNetClientUI.AvaloniaExtensions;
using SQRLDotNetClientUI.ViewModels;
using System.Collections.Generic;

namespace SQRLDotNetClientUI.Views
{
    public class MainMenuView : UserControl
    {
        public MainMenuView()
        {
            this.InitializeComponent();
            SetupLanguageMenu();
        }

        /// <summary>
        /// Adds all available language options to the main menu.
        /// </summary>
        private void SetupLanguageMenu()
        {
            // We need to instantiate a LocalizationExtension here
            // so that the can be sure that the localization data is initialized.
            LocalizationExtension _ = new LocalizationExtension();

            MenuItem languageMenu = this.FindControl<MenuItem>("menuLanguage");

            List<MenuItem> items = new List<MenuItem>();
            var newMenuView = new MainMenuViewModel();
            foreach (var lang in newMenuView.LanguageMenuItems)
            {
                MenuItem item = new MenuItem()
                {
                    Header = lang.Header,
                    Command = lang.Command,
                    CommandParameter = lang.CommandParameter,
                    Icon = lang.Icon
                };

                items.Add(item);
            }

            languageMenu.Items = items;

            //Derreference for GC
            newMenuView = null;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
