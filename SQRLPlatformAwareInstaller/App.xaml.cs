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
using ToolBox.Bridge;
using SQRLCommonUI.Models;

namespace SQRLPlatformAwareInstaller
{
    
    public class App : Application
    {
        private static IBridgeSystem _bridgeSystem { get; set; } = BridgeSystem.Bash;
        private static ShellConfigurator _shell { get; set; } = new ShellConfigurator(_bridgeSystem);
        private bool rootBail = false;
        public override void Initialize()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || 
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (!AdminCheck.IsAdmin())
                {
                    bool nogo=true;

                    //If Platform is Linux we can throw a hail mary and try to elevate the program by using the 
                    //polkit protocol via pkexe see: https://www.freedesktop.org/software/polkit/docs/0.105/pkexec.1.html
                    //In short the pkexec application allows you to request authorization and impersonation rights for an application
                    //in order for this to work you need to have an application speciific policy installed in the application (see Platform/Linux/Installer)
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Log.Information("Launched on Linux without Sudo, trying to re-launch if possible");
                        Log.Information("Checking if pkexec exists"); //This allows us to elevate a program
                        
                        //Checks to see if pkexec exists, if it doesn't bail #NothingWeCanDo
                        var result = _shell.Term("command -v pkexec", Output.Internal);
                        if(string.IsNullOrEmpty(result.stderr.Trim()) && !string.IsNullOrEmpty(result.stdout.Trim()))
                        {
                            //Checks to make sure that SQRL_HOME environment variable exists as well as the SQRLPlatformInstaller this is needed for the polkit invokation
                            string pkexec = result.stdout.Trim();
                            Log.Information("pkexec exists!");
                            
                            if(string.IsNullOrEmpty(result.stderr.Trim()) && File.Exists(Path.Combine("/usr/share/polkit-1/actions", "org.freedesktop.policykit.SQRLPlatformAwareInstaller_linux.policy")))
                            {
                                Log.Information("Found Installer in Path!");
                                
                                /*Polkit invokation forbids having a "dead" parent, so if we invoke polkit directly from here
                                  and then kill the process it will abort. First we need to write the polkit invocation
                                  to a shell script which is invoked externally so that we can kill our current instance of the installer cleanly.
                                */
                                var tmpScript = Path.GetTempFileName().Replace(".tmp",",sh");
                                Log.Information($"Copying Installer From:{Process.GetCurrentProcess().MainModule.FileName} to: /tmp/SQRLPlatformAwareInstaller_linux");
                                File.Copy(Process.GetCurrentProcess().MainModule.FileName, "/tmp/SQRLPlatformAwareInstaller_linux",true);
                                _shell.Term("chmod 777 /tmp/SQRLPlatformAwareInstaller_linux", Output.Hidden);
                                using (StreamWriter sw = new StreamWriter(tmpScript))
                                {
                                    sw.WriteLine("#!/bin/sh");
                                    sw.WriteLine($"{pkexec} /tmp/SQRLPlatformAwareInstaller_linux");
                                }
                                Log.Information($"Created launcher script at:{tmpScript}");
                                _shell.Term($"chmod a+x {tmpScript}");
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
                Log.Information($"Current Program: {Process.GetCurrentProcess().MainModule.FileName}");
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
