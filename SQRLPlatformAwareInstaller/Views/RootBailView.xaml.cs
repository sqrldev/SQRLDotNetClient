using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SQRLPlatformAwareInstaller.Views
{
    public class RootBailView : UserControl
    {
        public RootBailView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
