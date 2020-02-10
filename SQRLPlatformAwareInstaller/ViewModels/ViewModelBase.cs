using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        private string title = "";
        public string Title
        {
            get => this.title;
            set { this.RaiseAndSetIfChanged(ref title, value); }
        }
    }
}
