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
    /// This class implements a localizable message box to be used throughout the project.
    /// </summary>
    public class MessageBoxView : UserControl
    {
        /// <summary>
        /// Instanciates a new <c>MessageBoxView</c>.
        /// </summary>
        /// <param name="Title">The title of the message box</param>
        /// <param name="Message">The actual message to be delivered</param>
        /// <param name="messageBoxSize"> Optional size parameter changes the width of the window presented</param>
        /// <param name="messageBoxButtons">Optional (default "OK"). Allows you to select which buttons to present the user.</param>
        /// <param name="messageBoxIcon"> Optional (default "OK"). Allows you to select which icon to present the user with the message.</param>
        public MessageBoxView(string Title, string Message, MessageBoxSize messageBoxSize = MessageBoxSize.Medium, 
            MessageBoxButtons messageBoxButtons = MessageBoxButtons.OK, MessageBoxIcons messageBoxIcon = MessageBoxIcons.OK)
        {
            Init();
        }

        /// <summary>
        /// Instanciates a new <c>MessageBoxView</c>.
        /// </summary>
        public MessageBoxView()
        {
            Init();
        }

        /// <summary>
        /// Performs a few initialization tasks.
        /// </summary>
        private void Init()
        {
            AvaloniaLocator.CurrentMutable.Bind<MessageBoxView>().ToConstant(this);

            this.InitializeComponent();
            this.DataContextChanged += MessageBoxView_DataContextChanged;           
        }

        /// <summary>
        /// Event handler for the "DataContextChanged" event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event parameters.</param>
        private void MessageBoxView_DataContextChanged(object sender, EventArgs e)
        {
            if(this.DataContext !=null)
            {
                ((MessageBoxViewModel)this.DataContext).AddButtons();
            }
        }

        /// <summary>
        /// Initializes the UI components.
        /// </summary>
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    /// <summary>
    /// Public Enum which allows easy selection of MessageBox size (width).
    /// </summary>
    public enum MessageBoxSize
    {
        Small,
        Medium,
        Large,
        XLarge
    }

    /// <summary>
    /// Public Enum list of button combinations available to the MessageBox.
    /// These buttons automatically load the localization from the system if available.
    /// </summary>
    public enum MessageBoxButtons
    {
        OK,
        YesNo,
        OKCancel,
        Custom
    }

    /// <summary>
    /// Enum which determines which icon will be shown along with the message.
    /// </summary>
    public enum MessageBoxIcons
    {
        OK,
        ERROR,
        WARNING,
        QUESTION
    }

    /// <summary>
    /// Enum values which will be returned from a MessageBox based on which button is clicked.
    /// </summary>
    public enum MessagBoxDialogResult
    {
        OK,
        NO,
        YES,
        CANCEL,
        CUSTOM1,
        CUSTOM2,
        CUSTOM3,
        CUSTOM4
    }
}
