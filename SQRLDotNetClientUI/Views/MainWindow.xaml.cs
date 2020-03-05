using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SQRLDotNetClientUI.AvaloniaExtensions;

namespace SQRLDotNetClientUI.Views
{
    public class MainWindow : Window
    {
        public LocalizationExtension LocalizationService {get;}
        public MainWindow()
        {
            InitializeComponent();
            if (AvaloniaLocator.Current.GetService<MainWindow>() == null)
            {
                AvaloniaLocator.CurrentMutable.Bind<MainWindow>().ToConstant(this);
            }
            this.LocalizationService = new LocalizationExtension();
#if DEBUG
            this.AttachDevTools();
#endif

            // Prevent that closing the main form shuts down
            // the application and only hide the main window instead.
            this.Closing += (s, e) =>
            {
                ((Window)s).Hide();
                e.Cancel = true;
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
