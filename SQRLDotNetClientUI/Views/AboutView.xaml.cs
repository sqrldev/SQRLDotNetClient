using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SQRLDotNetClientUI.Views
{
    public class AboutView : UserControl
    {
        public AboutView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
