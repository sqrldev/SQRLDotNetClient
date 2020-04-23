using ReactiveUI;
using Serilog;
using System.Diagnostics;
using System.Reflection;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// A view model representing the app's "About" screen.
    /// </summary>
    public class AboutViewModel : ViewModelBase
    {
        private string _appVersion;

        /// <summary>
        /// Gets or sets the app's executable version.
        /// </summary>
        public string AppVersion
        {
            get => _appVersion;
            set => this.RaiseAndSetIfChanged(ref _appVersion, value);
        }

        /// <summary>
        /// Creates a new instance, sets the window title and performs
        /// a few initializations.
        /// </summary>
        public AboutViewModel()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();

            this.Title = _loc.GetLocalizationValue("AboutWindowTitle") + " " +
                assemblyName.Name;

            this.AppVersion = _loc.GetLocalizationValue("VersionLabel") + ": " +
                assemblyName.Version.ToString();
        }

        /// <summary>
        /// Navigates back to the app's main screen.
        /// </summary>
        public void Back()
        {
            Log.Information("Leaving about screen");

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
        }

        /// <summary>
        /// Starts a browser window with a link to the repo on Github.
        /// </summary>
        public void ShowRepository()
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.FileName = @"https://github.com/sqrldev/SQRLDotNetClient";
            p.Start();
        }

        /// <summary>
        /// Starts a browser window with a link to the repo on Github.
        /// </summary>
        public void ShowLicense()
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.FileName = @"https://raw.githubusercontent.com/sqrldev/SQRLDotNetClient/master/LICENSE";
            p.Start();
        }
    }
}
