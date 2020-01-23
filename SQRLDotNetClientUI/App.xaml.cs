using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SQRLDotNetClientUI.IPC;
using SQRLDotNetClientUI.ViewModels;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System.Threading;

namespace SQRLDotNetClientUI
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            Thread th = new Thread(StartNamePipe);
            th.Start();

        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                
            }

            base.OnFrameworkInitializationCompleted();
        }

        public void StartNamePipe()
        {
           
                IPCServer nps = new IPCServer("127.0.0.1", 13000);
                nps.StartServer();
            
        }
    }
}
