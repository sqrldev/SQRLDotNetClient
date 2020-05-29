using Avalonia;
using Avalonia.Platform;
using ReactiveUI;
using SQRLCommon.AvaloniaExtensions;
using SQRLPlatformAwareInstaller.Views;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        protected MainWindow _mainWindow = AvaloniaLocator.Current.GetService<MainWindow>();
        protected LocalizationExtension _loc = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService;
        protected IAssetLoader _assets = AvaloniaLocator.Current.GetService<IAssetLoader>();

        private string title = "";
        public string Title
        {
            get => this.title;
            set { this.RaiseAndSetIfChanged(ref title, value); }
        }
    }
}
