using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SQRLDotNetClient.ViewModels;

namespace SQRLDotNetClient.Views
{
    public class NewIdentityWindow : Window
    {
        public NewIdentityWindow()
        {
            this.StartUP();
        }
        public NewIdentityWindow(SQRLUtilsLib.SQRL instance)
        {
            this.DataContext = new NewIdentityModel(instance);
            this.StartUP();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void StartUP()
        {
            this.InitializeComponent();
            AvaloniaLocator.CurrentMutable.Bind<NewIdentityWindow>().ToConstant(this);
#if DEBUG
            this.AttachDevTools();
#endif
        }
    }
}
