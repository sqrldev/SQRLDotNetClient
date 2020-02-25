using Avalonia;
using ReactiveUI;
using SQRLDotNetClientUI.AvaloniaExtensions;
using SQRLDotNetClientUI.Views;

public class ViewModelBase : ReactiveObject
{
    protected MainWindow _mainWindow = AvaloniaLocator.Current.GetService<MainWindow>();
    protected LocalizationExtension _loc = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService;

    private string title="";
    public string Title
    {
        get => this.title; 
        set { this.RaiseAndSetIfChanged(ref title, value); }
    }
}