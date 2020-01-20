using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SQRLDotNetClient.ViewModels;

namespace SQRLDotNetClient.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            
            
            AvaloniaLocator.CurrentMutable.Bind<MainWindow>().ToConstant(this);

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
