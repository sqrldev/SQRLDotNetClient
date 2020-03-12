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

        public MessageBox(string Title, string Message, MessageBoxSize messageBoxSize = MessageBoxSize.Medium, MessageBoxButtons messageBoxButtons = MessageBoxButtons.OK, MessageBoxIcons messageBoxIcon = MessageBoxIcons.OK)
        {
            Init();
            this.DataContext = new MessageBoxModel(Title, Message, messageBoxSize, messageBoxButtons, messageBoxIcon);
        }

        public MessageBox()
        {
            Init();
            this.DataContext = new MessageBoxModel();
        }
        private void Init()
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            AvaloniaLocator.CurrentMutable.Bind<MessageBox>().ToConstant(this);

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
        Large,
        XLarge
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

    public enum MessagBoxDialogResult
    {
        OK,
        NO,
        YES,
        CANCEL
    }

    public class MessageBoxModel : ViewModelBase
    {
        public MessageBoxModel(string Title, string Message,MessageBoxSize messageBoxSize = MessageBoxSize.Medium, MessageBoxButtons messageBoxButtons = MessageBoxButtons.OK, MessageBoxIcons messageBoxIcon = MessageBoxIcons.OK)
        {
            this.Title = Title;
            this.Message = Message;
            switch (messageBoxSize)
            {
                case MessageBoxSize.XLarge:
                    this.Height = 280;
                    this.Width = 800;
                    this.MaxHeight = 60;
                    break;
                case MessageBoxSize.Large:
                    this.Height = 180;
                    this.Width = 600;
                    this.MaxHeight = 60;
                    break;
                case MessageBoxSize.Small:
                    this.Height = 180;
                    this.Width = 200;
                    this.MaxHeight = 60;
                    break;
                default:
                    {
                        this.Height = 180;
                        this.Width = 400;
                        this.MaxHeight = 60;
                    }
                    break;
            }

            internalIcon = messageBoxIcon switch
            {
                MessageBoxIcons.ERROR => "resm:SQRLDotNetClientUI.Assets.Icons.error.png",
                MessageBoxIcons.WARNING => "resm:SQRLDotNetClientUI.Assets.Icons.warning.png",
                _ => "resm:SQRLDotNetClientUI.Assets.Icons.ok.png",
            };
            switch (messageBoxButtons)
            {
                case MessageBoxButtons.OKCancel:
                {
                        AddButton("OK", _loc.GetLocalizationValue("BtnOK"), MessagBoxDialogResult.OK);
                        AddButton("Cancel", _loc.GetLocalizationValue("BtnCancel"), MessagBoxDialogResult.CANCEL);

                }
                break;
                case MessageBoxButtons.YesNo:
                    {
                        AddButton("Yes", _loc.GetLocalizationValue("BtnYes"), MessagBoxDialogResult.YES);
                        AddButton("No", _loc.GetLocalizationValue("BtnNo"), MessagBoxDialogResult.NO);
                    }
                    break;
                default:
                    {
                        AddButton("OK", _loc.GetLocalizationValue("BtnOK"), MessagBoxDialogResult.OK);
                    }
                    break;

            }

            Init();
        }

        private void AddButton(string name, string content, MessagBoxDialogResult clickResult)
        {
            var msgWindow = AvaloniaLocator.Current.GetService<MessageBox>();
            var panel = msgWindow.FindControl<StackPanel>("pnlButtons").Children;
            Button btn = new Button
            {
                Content = content,
                Name = name,
                Margin = new Thickness(10, 0),
                Width = 60
            };
            btn.Click += (s,e) => msgWindow.Close(clickResult); 
            panel.Add(btn);
        }

        private void Btn_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            throw new NotImplementedException();
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
        public int MaxHeight { get; set; } = 60;

        private string internalIcon { get; set; } = "resm:SQRLDotNetClientUI.Assets.Icons.ok.png";

        public Avalonia.Media.Imaging.Bitmap IconSource { get; set; }

    }
}
