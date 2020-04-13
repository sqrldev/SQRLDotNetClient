using Avalonia;
using ReactiveUI;
using SQRLCommonUI.AvaloniaExtensions;
using SQRLPlatformAwareInstaller.Views;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        protected LocalizationExtension _loc = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService;

        private string title = "";
        public string Title
        {
            get => this.title;
            set { this.RaiseAndSetIfChanged(ref title, value); }
        }
    }
}
