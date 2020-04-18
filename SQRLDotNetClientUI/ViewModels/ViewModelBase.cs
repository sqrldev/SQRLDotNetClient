using Avalonia;
using ReactiveUI;
using SQRLCommonUI.AvaloniaExtensions;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Views;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// A base class for all of the app's view models.
    /// </summary>
    public class ViewModelBase : ReactiveObject
    {
        private string title = "";

        /// <summary>
        /// The singleton <c>AppSettings</c> instance representing the 
        /// app's general settings.
        /// </summary>
        protected AppSettings _appSettings = AppSettings.Instance;

        /// <summary>
        /// The singleton <c>IdentityManager</c> instance.
        /// </summary>
        protected IdentityManager _identityManager = IdentityManager.Instance;

        /// <summary>
        /// The app's main window.
        /// </summary>
        protected MainWindow _mainWindow = AvaloniaLocator.Current.GetService<MainWindow>();

        /// <summary>
        /// An Avalonia extension providing localization/translation 
        /// services for the app.
        /// </summary>
        protected LocalizationExtension _loc = 
            AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService;

        /// <summary>
        /// The window title of the screen represented by the view model.
        /// </summary>
        public string Title
        {
            get => this.title;
            set { this.RaiseAndSetIfChanged(ref title, value); }
        }
    }
}