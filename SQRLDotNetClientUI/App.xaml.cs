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

            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string logFilePath = Path.Combine(currentDir, "log.txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("App starting, initialization completed!");
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Do not shutdown the application when the last window closes,
                // but only when receiving an explicit shutdown command.
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

                // Set up the app's main window
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
