using Avalonia.Threading;
using ReactiveUI;
using SQRLPlatformAwareInstaller.Models;
using System;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    /// <summary>
    /// A view model representing the Installer's "uninstall" screen.
    /// </summary>
    public class UninstallViewModel : ViewModelBase
    {
        private string _uninstallLog = "";
        private decimal _progressPercentage = 0;

        /// <summary>
        /// Gets or sets the uninstall log text.
        /// </summary>
        public string UninstallLog
        {
            get { return this._uninstallLog; }
            set { this.RaiseAndSetIfChanged(ref this._uninstallLog, value); }
        }

        /// <summary>
        /// Gets or sets the progress percentage.
        /// </summary>
        public decimal ProgressPercentage
        {
            get { return this._progressPercentage; }
            set { this.RaiseAndSetIfChanged(ref this._progressPercentage, value); }
        }

        /// <summary>
        /// Creates a new instance and performs some initialization tasks.
        /// </summary>
        public UninstallViewModel()
        {
            this.Title = _loc.GetLocalizationValue("TitleUninstall");
        }

        /// <summary>
        /// Starts the actual uninstall process.
        /// </summary>
        public async void Uninstall()
        {
            Progress<Tuple<int, string>> progress = new Progress<Tuple<int, string>>((x) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    this.ProgressPercentage = x.Item1;
                    this.UninstallLog = this.UninstallLog + Environment.NewLine + x.Item2;
                });
            });

            await Uninstaller.Run(progress, dryRun: false);
        }
    }
}
