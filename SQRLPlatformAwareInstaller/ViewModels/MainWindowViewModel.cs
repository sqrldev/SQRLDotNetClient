using ReactiveUI;
using Serilog;
using SQRLPlatformAwareInstaller.Models;
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

            
            if (InstallerCommands.Instance != null && InstallerCommands.Instance.Action == InstallerAction.Uninstall)
            {


                Log.Information($"Installer was called with \"{InstallerCommands.Instance}\" command line switches - launching uninstall screen");
                viewModel = new UninstallViewModel();
            }
            else if(InstallerCommands.Instance.Action== InstallerAction.Install)
            {
                viewModel = new MainInstallViewModel();
            }

            this.Content = viewModel;
        }
    }
}
