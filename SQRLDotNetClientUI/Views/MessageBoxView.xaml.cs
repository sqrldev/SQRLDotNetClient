using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using ReactiveUI;
using Avalonia.Platform;
using System.Collections.Concurrent;
using SQRLDotNetClientUI.ViewModels;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.Views
{
    /// <summary>
    /// This is a class which implements a message box to be used throughout the project
    /// it supports localization of buttons and dynamic re-sizing where allowed.
    /// </summary>
    public class MessageBoxView : UserControl
    {
        /// <summary>
        /// Instanciates a new MessageBoxWindow
        /// </summary>
        /// <param name="Title">The title of the messagebox</param>
        /// <param name="Message">The actual message to be delivered</param>
        /// <param name="messageBoxSize"> Optional Size Parameter changes the width of the window presented</param>
        /// <param name="messageBoxButtons">Optional (default OK) Allows you to select which buttons to give the user</param>
        /// <param name="messageBoxIcon"> Optioal (default OK) Allows you to select which Icon to present the user with the message</param>
        public MessageBoxView(string Title, string Message, MessageBoxSize messageBoxSize = MessageBoxSize.Medium, MessageBoxButtons messageBoxButtons = MessageBoxButtons.OK, MessageBoxIcons messageBoxIcon = MessageBoxIcons.OK)
        {
            Init();
            //this.DataContext = new MessageBoxViewModel(Title, Message, messageBoxSize, messageBoxButtons, messageBoxIcon);
        }

        public MessageBoxView()
        {
            Init();
            //this.DataContext = new MessageBoxViewModel();
        }
        private void Init()
        {
            

            AvaloniaLocator.CurrentMutable.Bind<MessageBoxView>().ToConstant(this);

            this.InitializeComponent();
            this.DataContextChanged += MessageBoxView_DataContextChanged;

            
        }

        private void MessageBoxView_DataContextChanged(object sender, EventArgs e)
        {
            if(this.DataContext !=null)
            {
                ((MessageBoxViewModel)this.DataContext).AddButtons();
            }
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
    public class MessageBoxViewModel : ViewModelBase
    {


        BlockingCollection<MessagBoxDialogResult> dialogResultCollection;
        ViewModelBase Parent;
        MessageBoxButtons messageBoxButtons;
        /// <summary>
        /// Instanciates a new MessageBoxModel
        /// </summary>
        /// <param name="Title">Message Box Header Title to be Displayed</param>
        /// <param name="Message">Actual Message to be displayed in the messageb box</param>
        /// <param name="messageBoxSize">MessageBox Size (Widht) Default Medium</param>
        /// <param name="messageBoxButtons">MessageBox Button Combiniation to Display (default OK)</param>
        /// <param name="messageBoxIcon">MessageBox Icon to Display (Default OK)</param>
        public MessageBoxViewModel(string Title, string Message,MessageBoxSize messageBoxSize = MessageBoxSize.Medium, MessageBoxButtons messageBoxButtons = MessageBoxButtons.OK, MessageBoxIcons messageBoxIcon = MessageBoxIcons.OK)
        {
            dialogResultCollection = new BlockingCollection<MessagBoxDialogResult>();
            this.Title = Title;
            this.Message = Message;
            this.messageBoxButtons = messageBoxButtons;

            internalIcon = messageBoxIcon switch
            {
                MessageBoxIcons.ERROR => "resm:SQRLDotNetClientUI.Assets.Icons.error.png",
                MessageBoxIcons.WARNING => "resm:SQRLDotNetClientUI.Assets.Icons.warning.png",
                MessageBoxIcons.QUESTION => "resm:SQRLDotNetClientUI.Assets.Icons.question.png",
                _ => "resm:SQRLDotNetClientUI.Assets.Icons.ok.png",
            };
            

            Init();
        }

        /// <summary>
        /// Adds a button to the pnlButtons panel in the message box
        /// </summary>
        /// <param name="name">name of the button</param>
        /// <param name="content">text to be shown on button</param>
        /// <param name="clickResult">Result to return when button is clicked</param>
        /// <param name="isDefault">If set to <c>true</c>, the button will be marked as the default
        /// button for the current form.</param>
        private void AddButton(string name, string content, MessagBoxDialogResult clickResult, bool isDefault=false)
        {
            var msgWindow = AvaloniaLocator.Current.GetService<MessageBoxView>();
            var panel = msgWindow.FindControl<StackPanel>("pnlButtons").Children;
            Button btn = new Button
            {
                Content = content,
                Name = name,
                Margin = new Thickness(10, 0),
                Width = 60,
                IsDefault = isDefault
            };
            btn.Click += (s,e) => { 
                                    ((MainWindowViewModel)this._mainWindow.DataContext).Content = Parent;
                                    dialogResultCollection.Add(clickResult);
                                   }; 
            panel.Add(btn);
        }

        public void AddButtons()
        {
            switch (messageBoxButtons)
            {
                case MessageBoxButtons.OKCancel:
                    {
                        AddButton("OK", _loc.GetLocalizationValue("BtnOK"), MessagBoxDialogResult.OK, isDefault: true);
                        AddButton("Cancel", _loc.GetLocalizationValue("BtnCancel"), MessagBoxDialogResult.CANCEL);

                    }
                    break;
                case MessageBoxButtons.YesNo:
                    {
                        AddButton("Yes", _loc.GetLocalizationValue("BtnYes"), MessagBoxDialogResult.YES, isDefault: true);
                        AddButton("No", _loc.GetLocalizationValue("BtnNo"), MessagBoxDialogResult.NO);
                    }
                    break;
                default:
                    {
                        AddButton("OK", _loc.GetLocalizationValue("BtnOK"), MessagBoxDialogResult.OK, isDefault: true);
                    }
                    break;

            }
        }

        public MessageBoxViewModel()
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

        public async Task<MessagBoxDialogResult> ShowDialog(ViewModelBase Parent)
        {
            this.Parent = Parent;
            ((MainWindowViewModel)this._mainWindow.DataContext).Content = this;

            return await Task.Run(() =>
            {
                foreach (var x in this.dialogResultCollection.GetConsumingEnumerable())
                    return x;
                return MessagBoxDialogResult.CANCEL;
            });
        }

    }
}
