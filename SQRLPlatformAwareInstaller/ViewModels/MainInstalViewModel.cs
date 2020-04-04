using Avalonia;
using SQRLPlatformAwareInstaller.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using Serilog;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    public class MainInstalViewModel: ViewModelBase
    {
        public string Greeting { get; set; }
        public MainInstalViewModel()
        {
            Log.Information("Installer Launched");
            this.Title = "SQRL Client Installer - Platform Selector";
            this.Greeting = AvaloniaLocator.Current.GetService<MainWindow>()==null?"Greeting": AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService.GetLocalizationValue("InstallerGreeting");
            SetPlatformImage();
            Log.Information($"Deltected Platform: {Platform}");
        }

        public string Platform { get
         {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "WINDOWS";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "MacOSX";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "Linux";
                else
                    return "";
            }
        }
        public void SetPlatformImage()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                this.PlatformImg = new Bitmap(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("resm:SQRLPlatformAwareInstaller.Assets.windows.png")));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                this.PlatformImg  =new Bitmap(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("resm:SQRLPlatformAwareInstaller.Assets.mac.png")));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                this.PlatformImg = new Bitmap(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("resm:SQRLPlatformAwareInstaller.Assets.linux.png")));
            else
                this.PlatformImg = new Bitmap(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("resm:SQRLPlatformAwareInstaller.Assets.unknown.png")));
        }

        private Bitmap _platformImg;
        public Bitmap PlatformImg
        {
            get
            {
                return _platformImg;
            }
            set { this.RaiseAndSetIfChanged(ref _platformImg, value); }
        }

        public void Next()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new VersionSelectorViewModel(this.Platform);
        }

        public void Cancel()
        {
            Environment.Exit(0);
        }

    }
}
