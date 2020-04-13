using ReactiveUI;
using System.Diagnostics;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    /// <summary>
    /// A view model representing the final confirmation screen
    /// of the SQRL installer.
    /// </summary>
    public class InstallationCompleteViewModel: ViewModelBase
    {
        private string _installPath = "";
        private bool _launchOnFinish = true;

        /// <summary>
        /// Gets or sets whether the SQRL client should be launched
        /// when the dialog is closed using the "finish" button.
        /// </summary>
        public bool LaunchOnFinish
        {
            get { return _launchOnFinish; }
            set { this.RaiseAndSetIfChanged(ref _launchOnFinish, value); }

        }

        /// <summary>
        /// Creates a new instance and initializes things.
        /// </summary>
        public InstallationCompleteViewModel()
        {
            Init();
        }

        public InstallationCompleteViewModel(string installPath)
        {
            this._installPath = installPath;
            Init();
        }

        /// <summary>
        /// Performs some common initialization like setting the dialog title etc.
        /// </summary>
        private void Init()
        {
            this.Title = _loc.GetLocalizationValue("TitleInstallationCompleteDialog");
        }

        /// <summary>
        /// Launches the SQRL client (if selected) and exits the installer.
        /// </summary>
        public void Finish()
        {
            if(this.LaunchOnFinish)
            {
                LaunchSQRL(this._installPath);
            }
            System.Environment.Exit(0);
        }

        /// <summary>
        /// Launches the freshly installed SQRL client app.
        /// </summary>
        /// <param name="installPath"></param>
        private void LaunchSQRL(string installPath)
        {
            var process = new Process();
            process.StartInfo.FileName = installPath;
            process.StartInfo.UseShellExecute = true;
            process.Start();
        }
    }
}
