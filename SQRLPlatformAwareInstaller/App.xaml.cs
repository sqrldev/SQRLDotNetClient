using System.IO;
using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Serilog;
using SQRLCommonUI.AvaloniaExtensions;
using SQRLPlatformAwareInstaller.ViewModels;
using SQRLPlatformAwareInstaller.Views;
using System.Runtime.InteropServices;
using SQRLCommonUI.Models;

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
                        Log.Information("Launched on Linux without sudo, trying to re-launch if possible");
                        Log.Information("Checking if pkexec exists"); //This allows us to elevate a program
                        
                        // Check if pkexec exists, if it doesn't bail #NothingWeCanDo
                        if(SystemAndShellUtils.IsPolKitAvailable())
                        {
                            Log.Information("PolicyKit available, pkexec exists!");

                            // Checks to make sure that SQRL_HOME environment variable exists as well as the SQRLPlatformInstaller 
                            // this is needed for the polkit invokation

                            if ( File.Exists(Path.Combine("/usr/share/polkit-1/actions", 
                                "org.freedesktop.policykit.SQRLPlatformAwareInstaller_linux.policy")))
                            {
                                Log.Information("Found existing PolicyKit policy file for Installer!");

                                // Copy the current installer to /tmp/ so that it can comply with polkit requirements. Note this 
                                // doesn't work correctly if you are in debug mode. In debug mode the file you are running is a dll, 
                                // not an executable, so be mindful of this

                                string currentLocation = Process.GetCurrentProcess().MainModule.FileName;
                                string tempLocation = $"/tmp/{CommonUtils.GetInstallerByPlatform()}";

                                if (currentLocation != tempLocation)
                                {
                                    Log.Information($"Copying Installer from \"{currentLocation}\" to \"{tempLocation}\"");
                                    File.Copy(Process.GetCurrentProcess().MainModule.FileName, "/tmp/SQRLPlatformAwareInstaller_linux", true);
                                    SystemAndShellUtils.Chmod("/tmp/SQRLPlatformAwareInstaller_linux", 777);
                                }

                                // PolKit invocation forbids having a "dead" parent, so if we invoke PolKit directly from here
                                // and then kill the process, it will abort. First we need to write the polkit invocation
                                // to a shell script which is invoked externally, so that we can kill our current instance of 
                                // the installer cleanly.

                                var tmpScript = Path.GetTempFileName().Replace(".tmp", ".sh");
                                using (StreamWriter sw = new StreamWriter(tmpScript))
                                {
                                    sw.WriteLine("#!/bin/sh");
                                    sw.WriteLine($"{SystemAndShellUtils.GetPolKitLocation()} {tempLocation}");
                                }
                                Log.Information($"Created launcher script at: {tmpScript}");
                                
                                SystemAndShellUtils.SetExecutableBit(tmpScript);

                                Process proc = new Process();
                                proc.StartInfo.FileName = tmpScript;
                                proc.Start();
                                nogo=false;
                            }
                        }
                    }

                    if (nogo)
                        rootBail = true;
                    else
                        Environment.Exit(0);    
                }
                Log.Information($"Current executable path: {Process.GetCurrentProcess().MainModule.FileName}");
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
                    DataContext = new MainWindowViewModel(rootBail),
                    Width = 600,
                    Height = 525
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
