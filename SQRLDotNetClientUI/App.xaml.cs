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
                // This also handles System Events and notifications, it gives us a native foothold on a Mac.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Log.Information("Initialializing Apple Delegate");
                    NSApplication.Init();
                    NSApplication.SharedApplication.Delegate = new  SQRLDotNetClientUI.Platform.OSX.AppDelegate();
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
