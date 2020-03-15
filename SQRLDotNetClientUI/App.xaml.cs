using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MonoMac.AppKit;
using SQRLDotNetClientUI.ViewModels;
using SQRLDotNetClientUI.Views;
using System.Runtime.InteropServices;
using Serilog;
using System.Reflection;
using System.IO;

namespace SQRLDotNetClientUI
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            Log.Information("App initialization completed!");
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
              
                // If this is running on a Mac we need a special event handler for URL schema Invokation
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    NSApplication.Init();
                    NSApplication.SharedApplication.Delegate = new  SQRLDotNetClientUI.Platform.OSX.AppDelegate((MainWindow)desktop.MainWindow);
                }

                  // Set up the app's main window
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };

                
            }          

            base.OnFrameworkInitializationCompleted();
        }

    }
}
