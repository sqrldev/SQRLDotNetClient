using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using ReactiveUI;
using Avalonia.Platform;

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

    public enum MessageBoxSize
    {
        Small,
        Medium,
        Large
    }

    public enum MessageBoxButtons
    {
        OK,
        YesNo,
        OKCancel
    }

    public enum MessageBoxIcons
    {
        OK,
        ERROR,
        WARNING
    }
    public class MessageBoxModel : ViewModelBase
    {
        public MessageBoxModel(string Title, string Message,MessageBoxSize messageBoxSize = MessageBoxSize.Medium, MessageBoxButtons messageBoxButtons = MessageBoxButtons.OK, MessageBoxIcons Icon = MessageBoxIcons.OK)
        {
            this.Title = Title;
            this.Message = Message;
            switch (messageBoxSize)
            {
                case MessageBoxSize.Large:
                    break;
                case MessageBoxSize.Small:
                    break;
            }
            Init();
        }
        public MessageBoxModel()
        {
            Init();
        }

        private void Init()
        {
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            this.IconSource = new Avalonia.Media.Imaging.Bitmap(assets.Open(new Uri(internalIcon)));
        }

        private string _message = "";
        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

  

        public int Height { get; set; } = 220;

        public int Width { get; set; } = 400;
        public int MaxHeight { get; set; } = 100;

        private string internalIcon { get; set; } = "resm:SQRLDotNetClientUI.Assets.Icons.ok.png";

        public Avalonia.Media.Imaging.Bitmap IconSource { get; set; }

    }
}
