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
using SQRLCommon.Models;

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
            if (!Directory.Exists(PathConf.LogPath)) Directory.CreateDirectory(PathConf.LogPath);
            string logFilePath = Path.Combine(PathConf.LogPath, "SQRLInstallerLog.log");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("New installer instance is being launched on {OSDescription}",
                RuntimeInformation.OSDescription);

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Log.Information($"Installer version: {version.ToString()}");

            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

            Log.Information($"Installer shutting down\r\n\r\n");
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
