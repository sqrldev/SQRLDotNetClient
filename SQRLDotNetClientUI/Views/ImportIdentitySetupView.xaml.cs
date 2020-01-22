using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SQRLDotNetClientUI.Views
{
    public class ImportIdentitySetupView : UserControl
    {
        public ImportIdentitySetupView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
