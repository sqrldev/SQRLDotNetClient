using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SQRLCommon.AvaloniaExtensions;

namespace SQRLPlatformAwareInstaller.Views
{
    public class MainWindow : Window
    {
        public LocalizationExtension LocalizationService { get; }
        public MainWindow()
        {
            InitializeComponent();
            if (AvaloniaLocator.Current.GetService<MainWindow>() == null)
            {
                AvaloniaLocator.CurrentMutable.Bind<MainWindow>().ToConstant(this);
            }
            this.LocalizationService = new LocalizationExtension();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
