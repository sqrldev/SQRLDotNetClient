using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SQRLPlatformAwareInstaller.Views
{
    public class InstallationCompleteView : UserControl
    {
        public InstallationCompleteView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
