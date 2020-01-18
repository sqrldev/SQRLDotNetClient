using SQRLDotNetClient.Views;

namespace SQRLDotNetClient.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Greeting => "Welcome to Avalonia!";

        public MainWindow CurrentWindow { get; set; }
        public SQRLUtilsLib.SQRL sqrlInstance { get; }

        public MainWindowViewModel()
        {
            this.sqrlInstance = new SQRLUtilsLib.SQRL(false);
        }
        public MainWindowViewModel(bool cps=false)
        {
            this.sqrlInstance = new SQRLUtilsLib.SQRL(cps);
        }

        public async void OnNewIdentityClick()
        {
            NewIdentityWindow w = new NewIdentityWindow(this.sqrlInstance);
           await w.ShowDialog(this.CurrentWindow);

        }
    }
}
