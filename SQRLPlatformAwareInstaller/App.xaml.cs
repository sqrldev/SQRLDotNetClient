using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SQRLPlatformAwareInstaller.ViewModels;
using SQRLPlatformAwareInstaller.Views;
using System.Runtime.InteropServices;

namespace SQRLPlatformAwareInstaller
{
  
    public class App : Application
    {
       
        public override void Initialize()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                if (!Utils.IsAdmin())
                    throw new System.Exception("This app must be run as an Administrator in Windows or Sudo/Root in Linux");
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
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
