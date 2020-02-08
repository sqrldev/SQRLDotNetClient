using Avalonia;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    class IdentitySettingsViewModel : ViewModelBase
    {
        public SQRL sqrlInstance { get; set; }
        public SQRLIdentity Identity { get; set; }
        public SQRLIdentity IdentityCopy { get; set; }

        public IdentitySettingsViewModel() { }

        public IdentitySettingsViewModel(SQRL sqrlInstance = null, SQRLIdentity identity = null)
        {
            this.Title = "SQRL Client - Identity Settings";
            this.sqrlInstance = sqrlInstance;
            this.Identity = identity;
            this.IdentityCopy = identity.Clone();

            if (identity != null) this.Title += " (" + identity.IdentityName + ")";
        }

        public void Close()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content =
                ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).MainMenu;
        }

        public void Save()
        {
            //TODO: Implement

            Close();
        }
    }
}
