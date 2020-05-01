using ReactiveUI;
using System;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    /// <summary>
    /// The view model for the installer's main window.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {   
        ViewModelBase content;
        ViewModelBase priorContent;

        /// <summary>
        /// The view model representing the currently visible content
        /// of the main window.
        /// </summary>
        public ViewModelBase Content
        {
            get => content;
            set { PriorContent = Content; this.RaiseAndSetIfChanged(ref content, value); }
        }

        /// <summary>
        /// The view model representing the previous content
        /// of the main window.
        /// </summary>
        public ViewModelBase PriorContent
        {
            get => priorContent;
            set => this.RaiseAndSetIfChanged(ref priorContent, value);
        }

        /// <summary>
        /// Creates a new instance and sets the content of the window.
        /// </summary>
        public MainWindowViewModel()
        {
            ViewModelBase viewModel = null;

            if (Environment.GetCommandLineArgs().Length > 1 &&
                Environment.GetCommandLineArgs()[1].ToLower() == "-uninstall")
            {
                viewModel = new UninstallViewModel();
            }
            else
            {
                viewModel = new MainInstallViewModel();
            }

            this.Content = viewModel;
        }
    }
}
