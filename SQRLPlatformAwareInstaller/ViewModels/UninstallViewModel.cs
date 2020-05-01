using Avalonia.Threading;
using ReactiveUI;
using SQRLPlatformAwareInstaller.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLPlatformAwareInstaller.ViewModels
{
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

        public UninstallViewModel()
        {
            this.Title = _loc.GetLocalizationValue("TitleUninstall");
        }

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

            await Uninstaller.Run(progress, dryRun: true);
        }
    }
}
