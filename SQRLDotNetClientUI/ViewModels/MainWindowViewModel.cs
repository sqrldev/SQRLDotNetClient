using ReactiveUI;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public SQRL sqrlInstance { get; set; }
        MainMenuViewModel mainMenu;
        ViewModelBase content;
        ViewModelBase priorContent;

        public MainMenuViewModel MainMenu
        {
            get => mainMenu;
            set {this.RaiseAndSetIfChanged(ref mainMenu, value); }
        }

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
            this.sqrlInstance = new SQRL(true);
            var mainMnu= new MainMenuViewModel(this.sqrlInstance);
            if (mainMnu.AuthVM != null)
                Content = mainMnu.AuthVM;
            else
                Content = mainMnu;

            MainMenu = mainMnu;
        }
    }
}
