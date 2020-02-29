using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        

        
        ViewModelBase content;
        ViewModelBase priorContent;

        public ViewModelBase Content
        {
            get => content;
            set { PriorContent = Content; this.RaiseAndSetIfChanged(ref content, value); }
        }

        public ViewModelBase PriorContent
        {
            get => priorContent;
            set => this.RaiseAndSetIfChanged(ref priorContent, value);
        }

        public MainWindowViewModel()
        {
            this.Content = new MainInstalViewModel();
           
        }

       
    }
}
