using ReactiveUI;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ToolBox.Bridge;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    /// <summary>
    /// A view model representing the final confirmation screen
    /// of the SQRL installer.
    /// </summary>
    public class InstallationCompleteViewModel: ViewModelBase
    {
        private string _clientExePath = "";
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

        /// <summary>
        /// Creates a new instance and passes in the <paramref name="clientExePath"/>.
        /// </summary>
        /// <param name="clientExePath">The full path to the client executable.</param>
        public InstallationCompleteViewModel(string clientExePath)
        {
            this._clientExePath = clientExePath;
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
            if (this.LaunchOnFinish)
            {
                LaunchSQRL();
            }
            System.Environment.Exit(0);
        }

        /// <summary>
        /// Launches the freshly installed SQRL client app.
        /// </summary>
        private void LaunchSQRL()
        {
            Log.Information($"Launching client from installer");
            var process = new Process();
            process.StartInfo.FileName = _clientExePath;
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(_clientExePath);
            process.StartInfo.UseShellExecute = true;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ShellConfigurator shell = new ShellConfigurator(BridgeSystem.Bash);
                string user = shell.Term("logname", Output.Hidden).stdout.Trim();
                process.StartInfo.UserName = user;
                process.StartInfo.UseShellExecute = false;
            }
            process.Start();
        }
    }
}
