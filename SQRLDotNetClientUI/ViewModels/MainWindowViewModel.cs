using ReactiveUI;
using SQRLDotNetClientUI.Views;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// A view model representing the main application window.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        MainMenuViewModel _mainMenu;
        ViewModelBase _content;
        ViewModelBase _priorContent;

        /// <summary>
        /// Gets or sets the view model representing the app's main screen.
        /// </summary>
        public MainMenuViewModel MainMenu
        {
            get => _mainMenu;
            set { this.RaiseAndSetIfChanged(ref _mainMenu, value); }
        }

        /// <summary>
        /// Gets or sets the view model of the corresponding view that will 
        /// be displayed within the app's main window.
        /// </summary>
        public ViewModelBase Content
        {
            get => _content;
            set 
            { 
                // Don't want PriorContent to be messed up because of the progress indicator
                // or message boxes so we aren't counting those
                if (Content != null &&
                    Content != PriorContent &&
                    Content.GetType() != typeof(ProgressDialogViewModel) &&
                    Content.GetType() != typeof(MessageBoxViewModel) &&
                    value.GetType() != typeof(ProgressDialogViewModel) &&
                    value.GetType() != typeof(MessageBoxViewModel))
                {
                    PriorContent = Content;
                }
                 
                this.RaiseAndSetIfChanged(ref _content, value); 
            }
        }

        /// <summary>
        /// Gets or sets the view model of the corresponding view that was 
        /// previously displayed within the app's main window.
        /// </summary>
        public ViewModelBase PriorContent
        {
            get => _priorContent;
            set => this.RaiseAndSetIfChanged(ref _priorContent, value);
        }

        /// <summary>
        /// Creates a new <c>MainWindowViewModel</c> instance and performs some 
        /// initialization tasks.
        /// </summary>
        public MainWindowViewModel()
        {
            var mainMnu = new MainMenuViewModel();
            if (mainMnu != null && mainMnu.AuthVM != null)
                Content = mainMnu.AuthVM;
            else
                Content = mainMnu;

            MainMenu = mainMnu;
        }
    }
}
