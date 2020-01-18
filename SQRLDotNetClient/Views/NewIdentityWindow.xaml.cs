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
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }
        public NewIdentityWindow(SQRLUtilsLib.SQRL instance)
        {
            this.DataContext = new NewIdentityModel(instance);
            this.InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
