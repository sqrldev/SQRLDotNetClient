using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SQRLCommon.AvaloniaExtensions;
using SQRLDotNetClientUI.ViewModels;

namespace SQRLDotNetClientUI.Views
{
    /// <summary>
    /// A dialog window that prompts the user to input a secret.
    /// </summary>
    public class InputSecretDialogView : UserControl
    {
        private LocalizationExtension _loc = (App.Current as App).Localization;
        private TextBox _txtSecret = null;
        private TextBlock _lblMessage = null;
        
        /// <summary>
        /// Creates a new <c>InputSecretDialogView</c> instance and sets 
        /// the type of secret that the user will be asked to input to 
        /// <c>SecretType.Password</c>.
        /// </summary>
        public InputSecretDialogView() : this(SecretType.Password) { }

        /// <summary>
        /// Creates a new <c>InputSecretDialogView</c> instance and 
        /// specifies the type of secret that the user will be asked to input.
        /// </summary>
        /// <param name="secretType">Tht type of secret that the user should input (password, rescue code...).</param>
        public InputSecretDialogView(SecretType secretType)
        {
            this.InitializeComponent();
            this.DataContextChanged += InputSecretDialogView_DataContextChanged;
            
            _txtSecret = this.FindControl<CopyPasteTextBox> ("txtSecret");
            _lblMessage = this.FindControl<TextBlock>("lblMessage");

            // This is only here because for some reason the XAML behaviour "FocusOnAttached"
            // isn't working for this window. No clue why, probably a dumb mistake on my part.
            // If anyone gets this working in XAML, this ugly hack can be removed!
            _txtSecret.AttachedToVisualTree += (sender, e) => (sender as CopyPasteTextBox).Focus();
        }

        /// <summary>
        /// Event handler for the "DataContextChanged" event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void InputSecretDialogView_DataContextChanged(object sender, System.EventArgs e)
        {
            if(this.DataContext!=null)
            {
                var ipdm = (InputSecretDialogViewModel)this.DataContext;
                switch (ipdm.SecretType)
                {
                    case SecretType.Password:
                        _lblMessage.Text = _loc.GetLocalizationValue("EnterPasswordMessage");
                        _txtSecret.PasswordChar = '*';
                        break;

                    case SecretType.RescueCode:
                        _lblMessage.Text = _loc.GetLocalizationValue("EnterRescueCodeMessage");
                        break;

                    default:
                        break;
                }
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
    /// Specifies the type of secret that the user should
    /// be asked to enter.
    /// </summary>
    public enum SecretType
    {
        Password,
        RescueCode
    }   
}
