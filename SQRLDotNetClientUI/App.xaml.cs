using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MonoMac.AppKit;
using SQRLDotNetClientUI.IPC;
using SQRLDotNetClientUI.ViewModels;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System.Runtime.InteropServices;
using System.Threading;

namespace SQRLDotNetClientUI
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };

                // If this is running on a Mac we need a special event handler for URL schema Invokation
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    NSApplication.Init();
                    NSApplication.SharedApplication.Delegate = new Utils.AppDelegate((MainWindow)desktop.MainWindow);
                }

            }

            

            base.OnFrameworkInitializationCompleted();
        }

    }
}
