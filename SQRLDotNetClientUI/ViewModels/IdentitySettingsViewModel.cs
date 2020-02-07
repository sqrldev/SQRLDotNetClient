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

        public IdentitySettingsViewModel(SQRL sqrlInstance = null, SQRLIdentity identity = null)
        {
            this.Title = "SQRL Client - Identity Settings";
            this.sqrlInstance = sqrlInstance;
            this.Identity = identity;
        }
    }
}
