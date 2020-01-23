using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SQRLDotNetClientUI.Views
{
    public class MainMenuView : UserControl
    {
        public MainMenuView()
        {
            this.InitializeComponent();
            
        }

      

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }


    }
}
