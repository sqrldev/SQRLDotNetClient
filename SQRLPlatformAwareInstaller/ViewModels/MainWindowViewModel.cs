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
        /// <param name="rootBail">When this parameter is passed (true) it tells the installer to abort and 
        /// presents a warning to the user regarding sudo/root requirement</param>
        public MainWindowViewModel(bool rootBail = false)
        {
            ViewModelBase viewModel = null;

            Log.Information($"Installer was called with these command line switches: {CommandLineArgs.Instance}");

            if (rootBail)
            {
                Log.Information("Launching \"root bail\" screen");
                viewModel = new RootBailViewModel();
            }
            else if (CommandLineArgs.Instance.Action == InstallerAction.Uninstall)
            {
                Log.Information("Launching uninstall screen");
                viewModel = new UninstallViewModel();
            }
            else if (CommandLineArgs.Instance.Action == InstallerAction.Update &&
                     !string.IsNullOrEmpty(CommandLineArgs.Instance.ZipFilePath) &&
                     !string.IsNullOrEmpty(CommandLineArgs.Instance.VersionTag))
            {
                Log.Information("Launching version selector screen with update zip path");
                viewModel = new VersionSelectorViewModel(CommandLineArgs.Instance.ZipFilePath,
                    CommandLineArgs.Instance.VersionTag);
            }
            else
            {
                Log.Information("Launching main install screen");
                viewModel = new MainInstallViewModel();
            }

            this.Content = viewModel;
        }
    }
}
