using Avalonia;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    public class InstallationCompleteViewModel: ViewModelBase
    {
        private string installPath = "";


        private bool _LaunchOnFinish = true;
        public bool LaunchOnFinish
        {
            get { return _LaunchOnFinish; }
            set { this.RaiseAndSetIfChanged(ref _LaunchOnFinish, value); }

        }
        public InstallationCompleteViewModel()
        {
            this.Title = "SQRL Client Installer - Installation Complete";
        }

        public InstallationCompleteViewModel(string installPath)
        {
            this.installPath = installPath;
            this.Title = "SQRL Client Installer - Installation Complete";
        }

        public void Finish()
        {
            if(this.LaunchOnFinish)
            {
                LaunchSQRL(this.installPath);
            }
            System.Environment.Exit(0);
        }

        private void LaunchSQRL(string installPath)
        {
            var process = new Process();
            process.StartInfo.FileName = installPath;
            process.StartInfo.UseShellExecute = true;
            process.Start();
            
        }
    }
}
