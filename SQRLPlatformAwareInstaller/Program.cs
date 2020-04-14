using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using Serilog;
using Avalonia.Dialogs;
namespace SQRLPlatformAwareInstaller
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            // Set up logging
            string currentDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string logFilePath = Path.Combine(currentDir, "SQRLInstallerLog.log");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("New app instance is being launched on {OSDescription}",
                RuntimeInformation.OSDescription);

            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new AvaloniaNativePlatformOptions { UseGpu = !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) })
                .LogToDebug()
                .UseReactiveUI()
                .UseManagedSystemDialogs(); //It is recommended by Avalonia Developers that we use Managed System Dialogs instead  of the native ones particularly for Linux

    }
}
