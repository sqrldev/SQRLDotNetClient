using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SQRLDotNetClient.ViewModels;

namespace SQRLDotNetClient.Views
{
    public class NewIdentityVerifyWindow : Window
    {
        public NewIdentityVerifyWindow(SQRLUtilsLib.SQRL sqrlInstance, SQRLUtilsLib.SQRLIdentity identity)
        {
            this.StartUp();
            this.DataContext = new NewIdentityVerifyModel(sqrlInstance, identity);
            
        }
        public NewIdentityVerifyWindow()
        {
            this.StartUp();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void StartUp()
        {
            this.InitializeComponent();
            AvaloniaLocator.CurrentMutable.Bind<NewIdentityVerifyWindow>().ToConstant(this);
#if DEBUG
            this.AttachDevTools();
#endif
        }
    }
}
