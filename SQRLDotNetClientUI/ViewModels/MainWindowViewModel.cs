using ReactiveUI;
using SQRLDotNetClientUI.Platform.Win;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        MainMenuViewModel mainMenu;
        ViewModelBase content;
        ViewModelBase priorContent;

        public MainMenuViewModel MainMenu
        {
            get => mainMenu;
            set { this.RaiseAndSetIfChanged(ref mainMenu, value); }
        }

        public ViewModelBase Content
        {
            get => content;
            set { 
                // Don't want PriorContent to be messed up because of the progress indicator so we aren't counting those
                PriorContent = (value.GetType() != typeof(ProgressDialogViewModel) && Content!=null && Content.GetType() != typeof(ProgressDialogViewModel) ? Content : PriorContent); 
                this.RaiseAndSetIfChanged(ref content, value); 
            }
        }

        public ViewModelBase PriorContent
        {
            get => priorContent;
            set => this.RaiseAndSetIfChanged(ref priorContent, value);
        }

        public MainWindowViewModel()
        {
            var mainMnu = new MainMenuViewModel();
            if (mainMnu != null && mainMnu.AuthVM != null)
                Content = mainMnu.AuthVM;
            else
                Content = mainMnu;

            MainMenu = mainMnu;
        }
    }
}
