using Avalonia;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;

public class ViewModelBase : ReactiveObject
{
    private string title="";
    public string Title
    {
        get => this.title; 
        set { this.RaiseAndSetIfChanged(ref title, value); }
    }

    
}