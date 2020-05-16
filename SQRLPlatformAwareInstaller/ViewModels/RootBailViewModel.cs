using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    public class RootBailViewModel: ViewModelBase
    {
        public RootBailViewModel()
        {
            this.Title = _loc.GetLocalizationValue("BailTitle");
        }
        public void Leave()
        {
            Environment.Exit(0);
        }

    }
}
