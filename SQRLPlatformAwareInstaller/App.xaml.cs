using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SQRLCommonUI.AvaloniaExtensions;
using SQRLPlatformAwareInstaller.ViewModels;
using SQRLPlatformAwareInstaller.Views;
using System.Runtime.InteropServices;

namespace SQRLPlatformAwareInstaller
{
    public class App : Application
    {
        public override void Initialize()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || 
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (!Utils.IsAdmin())
                {
                    throw new System.Exception("This app must be run as an administrator in Windows or sudo/root in Linux");
                }
            }

            AvaloniaXamlLoader.Load(this);

            // This is here only to be able to manually load a specific translation 
            // during development by setting CurrentLocalization it to something 
            // like "en-US" or "de-DE";
            LocalizationExtension loc = new LocalizationExtension();
            LocalizationExtension.CurrentLocalization = LocalizationExtension.DEFAULT_LOC; 
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                    Width = 600,
                    Height = 525
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
