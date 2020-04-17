using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// A view model representing the app's "Ask" screen.
    /// </summary>
    public class AskViewModel: ViewModelBase
    {
        private string askButton1 = "Button 1";
        private string askButton2 = "Button 2";

        /// <summary>
        /// Gets or sets the site (domain) that the ask message is coming from.
        /// </summary>
        public Uri Site { get; set; }

        /// <summary>
        /// Gets or sets the latest response from the SQRL server.
        /// </summary>
        public SQRLServerResponse ServerResponse { get; set; }

        /// <summary>
        /// Gets or sets the question text coming from the server
        /// through the ask protocol feature.
        /// </summary>
        public String AskMessage { get; set; } = "Hey, are you sure you want to do the thing and what not?";

        /// <summary>
        /// Gets or sets the window that is housing the ask view/view model.
        /// </summary>
        public MainWindow CurrentWindow { get; set; }

        /// <summary>
        /// Gets or sets the text/catption for button 1.
        /// </summary>
        public String AskButton1
        {
            get => askButton1;
            set => this.RaiseAndSetIfChanged(ref askButton1, value);
        }

        /// <summary>
        /// Gets or sets the text/catption for button 2.
        /// </summary>
        public String AskButton2
        {
            get => askButton2;
            set => this.RaiseAndSetIfChanged(ref askButton2, value);
        }

        /// <summary>
        /// Gets or sets a value indicating which of the buttons was
        /// pressed by the user.
        /// </summary>
        public int ButtonValue { get; set; }

        /// <summary>
        /// Creates a new <c>AskViewModel</c> instance and initializes things.
        /// </summary>
        public AskViewModel()
        {
            Init();
        }

        /// <summary>
        /// Creates a new <c>AskViewModel</c> instance, passing in the
        /// latest <paramref name="serverResponse"/>.
        /// </summary>
        /// <param name="serverResponse">The last response from the SQRL server.</param>
        public AskViewModel(SQRLServerResponse serverResponse)
        {
            Init();
            this.ServerResponse = serverResponse;
            this.AskMessage = serverResponse.AskMessage;
            this.AskButton1 = serverResponse.GetAskButtons[0];
            this.AskButton2 = serverResponse.GetAskButtons.Length > 1 ? serverResponse.GetAskButtons[1] : string.Empty;
        }

        /// <summary>
        /// Performs initialization tasks such as setting the window title.
        /// </summary>
        private void Init()
        {
            this.Title = _loc.GetLocalizationValue("AskWindowTitle");
        }

        /// <summary>
        /// Event handler for button #1.
        /// </summary>
        public void Button1()
        {
            this.ButtonValue = 1;
            this.CurrentWindow.Close(this.ButtonValue);
        }

        /// <summary>
        /// Event handler for button #2.
        /// </summary>
        public void Button2()
        {
            this.ButtonValue = 2;
            this.CurrentWindow.Close(this.ButtonValue);
        }
    }
}
