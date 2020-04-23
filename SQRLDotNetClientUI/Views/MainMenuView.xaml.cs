using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SQRLCommonUI.AvaloniaExtensions;
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
            LocalizationExtension loc = new LocalizationExtension();

            MenuItem languageMenu = this.FindControl<MenuItem>("menuLanguage");

            var mmvm = ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).MainMenu;
            List<MenuItem> items = loc.GetLanguageMenuItems(mmvm);

            languageMenu.Items = items;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
