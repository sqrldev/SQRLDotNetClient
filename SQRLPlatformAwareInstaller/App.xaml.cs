using System.IO;
using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Serilog;
using SQRLCommon.AvaloniaExtensions;
using SQRLPlatformAwareInstaller.ViewModels;
using SQRLPlatformAwareInstaller.Views;
using System.Runtime.InteropServices;
using SQRLCommon.Models;

namespace SQRLPlatformAwareInstaller
{
    
    public class App : Application
    {
        private bool rootBail = false;

        public override void Initialize()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || 
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (!SystemAndShellUtils.IsAdmin())
                {
                    bool nogo=true;

                    // If platform is Linux we can throw a hail mary and try to elevate the program by using the 
                    // polkit protocol via pkexe, see: https://www.freedesktop.org/software/polkit/docs/0.105/pkexec.1.html
                    // In short, the pkexec application allows you to request authorization and impersonation rights 
                    // for an application. In order for this to work, you need to have an application specific policy 
                    // installed in the application (see Platform/Linux/Installer).

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Log.Information("Launched on Linux without sudo, trying to re-launch using PolicyKit");
                        nogo = !SystemAndShellUtils.LaunchInstallerUsingPolKit(copyCurrentProcessExecutable: true);
                    }

                    if (nogo) 
                        rootBail = true;
                    else 
                        Environment.Exit(0);
                }
            }

            Log.Information($"Current executable path: {Process.GetCurrentProcess().MainModule.FileName}");
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
                    DataContext = new MainWindowViewModel(rootBail),
                    Width = 600,
                    Height = 525
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
