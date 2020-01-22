using Avalonia;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    public class MainMenuViewModel : ViewModelBase
    {
        public SQRL sqrlInstance { get; set; }
        public SQRLIdentity currentIdentity { get; set; }

        public MainMenuViewModel()
        {
            this.Title = "SQRL Client";
        }

        public void OnNewIdentityClick()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new NewIdentityViewModel(this.sqrlInstance);
        }

        public void ExportIdentity()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new ExportIdentityViewModel(this.sqrlInstance, this.currentIdentity);
        }

        public void ImportIdentity()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new ImportIdentityViewModel(this.sqrlInstance);
        }
    }

   
}
