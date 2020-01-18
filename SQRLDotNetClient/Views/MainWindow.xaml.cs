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
            
            if (this.DataContext != null)
            {
                var vm = (MainWindowViewModel)this.DataContext;
                vm.CurrentWindow = this;
                
            }
            this.DataContextChanged += MainWindow_DataContextChanged;
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void MainWindow_DataContextChanged(object sender, System.EventArgs e)
        {
            if (this.DataContext != null)
            {
                var vm = (MainWindowViewModel)this.DataContext;
                vm.CurrentWindow = this;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        
    }
}
