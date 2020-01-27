using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SQRLDotNetClientUI.ViewModels;

namespace SQRLDotNetClientUI.Views
{
    public class AuthenticationView : UserControl
    {
        public AuthenticationView()
        {
            this.InitializeComponent();
            
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
