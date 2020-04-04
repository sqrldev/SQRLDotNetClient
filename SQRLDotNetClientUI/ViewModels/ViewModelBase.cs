using Avalonia;
using ReactiveUI;
using SQRLCommonUI.AvaloniaExtensions;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Views;

public class ViewModelBase : ReactiveObject
{
    protected IdentityManager _identityManager = IdentityManager.Instance;
    protected MainWindow _mainWindow = AvaloniaLocator.Current.GetService<MainWindow>();
    protected LocalizationExtension _loc = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService;

    private string title="";
    public string Title
    {
        get => this.title; 
        set { this.RaiseAndSetIfChanged(ref title, value); }
    }
}