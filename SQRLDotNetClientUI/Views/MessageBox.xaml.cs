using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using ReactiveUI;
using Avalonia.Platform;

namespace SQRLDotNetClientUI.Views
{
    /// <summary>
    /// This is a class which implements a message box to be used throughout the project
    /// it supports localization of buttons and dynamic re-sizing where allowed.
    /// </summary>
    public class MessageBox : Window
    {
        /// <summary>
        /// Instanciates a new MessageBoxWindow
        /// </summary>
        /// <param name="Title">The title of the messagebox</param>
        /// <param name="Message">The actual message to be delivered</param>
        /// <param name="messageBoxSize"> Optional Size Parameter changes the width of the window presented</param>
        /// <param name="messageBoxButtons">Optional (default OK) Allows you to select which buttons to give the user</param>
        /// <param name="messageBoxIcon"> Optioal (default OK) Allows you to select which Icon to present the user with the message</param>
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

    /// <summary>
    /// Public Enum which allows easy selection of Message Box Size (Width)
    /// </summary>
    public enum MessageBoxSize
    {
        Small,
        Medium,
        Large,
        XLarge
    }

    /// <summary>
    /// Public Enum List of Button Combinations Available to the MessageBox.
    /// These buttons automatically load the localization from the system if available
    /// </summary>
    public enum MessageBoxButtons
    {
        OK,
        YesNo,
        OKCancel
    }

    /// <summary>
    /// Enum which determines which Icon will be shown along with the message box
    /// </summary>
    public enum MessageBoxIcons
    {
        OK,
        ERROR,
        WARNING,
        QUESTION
    }

    /// <summary>
    /// Enum values which will be returned from a messagebox based on which button you click
    /// </summary>
    public enum MessagBoxDialogResult
    {
        OK,
        NO,
        YES,
        CANCEL
    }


    /// <summary>
    /// This Class is used as the binding model for the MessageBoxWindow 
    /// </summary>
    public class MessageBoxModel : ViewModelBase
    {
        /// <summary>
        /// Instanciates a new MessageBoxModel
        /// </summary>
        /// <param name="Title">Message Box Header Title to be Displayed</param>
        /// <param name="Message">Actual Message to be displayed in the messageb box</param>
        /// <param name="messageBoxSize">MessageBox Size (Widht) Default Medium</param>
        /// <param name="messageBoxButtons">MessageBox Button Combiniation to Display (default OK)</param>
        /// <param name="messageBoxIcon">MessageBox Icon to Display (Default OK)</param>
        public MessageBoxModel(string Title, string Message,MessageBoxSize messageBoxSize = MessageBoxSize.Medium, MessageBoxButtons messageBoxButtons = MessageBoxButtons.OK, MessageBoxIcons messageBoxIcon = MessageBoxIcons.OK)
        {
            this.Title = Title;
            this.Message = Message;
            switch (messageBoxSize)
            {
                case MessageBoxSize.XLarge:
                    
                    this.Width = 800;
                    
                    break;
                case MessageBoxSize.Large:
                    
                    this.Width = 600;
                    
                    break;
                case MessageBoxSize.Small:
                    
                    this.Width = 200;
                    
                    break;
                default:
                    {
                        
                        this.Width = 400;
                        
                    }
                    break;
            }

            internalIcon = messageBoxIcon switch
            {
                MessageBoxIcons.ERROR => "resm:SQRLDotNetClientUI.Assets.Icons.error.png",
                MessageBoxIcons.WARNING => "resm:SQRLDotNetClientUI.Assets.Icons.warning.png",
                MessageBoxIcons.QUESTION => "resm:SQRLDotNetClientUI.Assets.Icons.question.png",
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

        /// <summary>
        /// Adds a button to the pnlButtons panel in the message box
        /// </summary>
        /// <param name="name">name of the button</param>
        /// <param name="content">text to be shown on button</param>
        /// <param name="clickResult">Result to return when button is clicked</param>
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

  

        
        /// <summary>
        /// Determines the width of the form
        /// </summary>
        public int Width { get; set; } = 400;
        

        private string internalIcon { get; set; } = "resm:SQRLDotNetClientUI.Assets.Icons.ok.png";

        public Avalonia.Media.Imaging.Bitmap IconSource { get; set; }

    }
}
