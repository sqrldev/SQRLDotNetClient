using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using SQRLCommonUI.Models;
using SQRLPlatformAwareInstaller.Models;
using SQRLPlatformAwareInstaller.Platform;
using System;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    /// <summary>
    /// A view model representing the Installer's "uninstall" screen.
    /// </summary>
    public class UninstallViewModel : ViewModelBase
    {
        private IInstaller _installer = null;
        private string _uninstallLog = "";
        private string _uninstallButtonText = "";
        private decimal _progressPercentage = 0;
        private bool _canUninstall = true;
        private bool _uninstallFinished = false;

        /// <summary>
        /// Gets or sets the uninstall log text.
        /// </summary>
        public string UninstallLog
        {
            get { return this._uninstallLog; }
            set { this.RaiseAndSetIfChanged(ref this._uninstallLog, value); }
        }

        /// <summary>
        /// Gets or sets the uninstall button text.
        /// </summary>
        public string UninstallButtonText
        {
            get { return this._uninstallButtonText; }
            set { this.RaiseAndSetIfChanged(ref this._uninstallButtonText, value); }
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
        /// Gets or sets a value indicating whether the uninstall button should be enabled.
        /// </summary>
        public bool CanUninstall
        {
            get { return this._canUninstall; }
            set { this.RaiseAndSetIfChanged(ref this._canUninstall, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the uninstall process has finished.
        /// </summary>
        public bool UninstallFinished
        {
            get { return this._uninstallFinished; }
            set 
            { 
                this.UninstallButtonText = value ?
                    _loc.GetLocalizationValue("BtnFinish") :
                    _loc.GetLocalizationValue("BtnUninstall");

                this.RaiseAndSetIfChanged(ref this._uninstallFinished, value); 
            }
        }

        /// <summary>
        /// Creates a new instance and performs some initialization tasks.
        /// </summary>
        public UninstallViewModel()
        {
            this.Title = _loc.GetLocalizationValue("TitleUninstall");
            this.UninstallButtonText = _loc.GetLocalizationValue("BtnUninstall");

            // Create a platform-specific installer instance
            _installer = Activator.CreateInstance(
                Implementation.ForType<IInstaller>()) as IInstaller;
        }

        /// <summary>
        /// Starts the actual uninstall process.
        /// </summary>
        public async void Uninstall()
        {
            // If "Uninstall()" was already run before, this method gets called again when 
            // the user hits the "finish" button. If that's the case, just drop out. 
            if (this.UninstallFinished) Cancel();
            
            // If we get here, we're about to launch the actual uninstall
            Progress<Tuple<int, string>> progress = new Progress<Tuple<int, string>>((x) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    this.ProgressPercentage = x.Item1;
                    this.UninstallLog = this.UninstallLog + Environment.NewLine + x.Item2;
                });
            });

            this.CanUninstall = false;
            await _installer.Uninstall(progress, dryRun: false);
            this.UninstallFinished = true;
            this.CanUninstall = true;
        }

        /// <summary>
        /// Closes the installer.
        /// </summary>
        public void Cancel()
        {
            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                .Shutdown();
        }
    }
}
