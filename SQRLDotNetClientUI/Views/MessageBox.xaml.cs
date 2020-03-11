using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using ReactiveUI;
namespace SQRLDotNetClientUI.Views
{
    public class MessageBox : Window
    {

        
        public MessageBox()
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
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

    public class MessageBoxModel: ViewModelBase
    {
       public MessageBoxModel()
        {
            this.Title = "Message Box";
      
        }

        private string _message = "";
        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        public MessageBoxModel(string Title, string Message)
        {
            this.Title = Title;
            this.Message = Message;
        }
    }
}
