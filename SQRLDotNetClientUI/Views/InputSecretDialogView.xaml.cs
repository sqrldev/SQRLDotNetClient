using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SQRLDotNetClientUI.AvaloniaExtensions;

namespace SQRLDotNetClientUI.Views
{
    /// <summary>
    /// A dialog window that prompts the user to input a secret.
    /// </summary>
    public class InputSecretDialogView : Window
    {
        private MainWindow _mainWindow = AvaloniaLocator.Current.GetService<MainWindow>();
        private LocalizationExtension _loc = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService;
        private TextBox _txtSecret = null;
        private Button _btnOK = null;
        private TextBlock _lblMessage = null;
        private SecretType _secretType;
        private bool AllGood = false;
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

            //Prevent closing the dialog externally.
            this.Closing += InputSecretDialogView_Closing;
            _txtSecret = this.FindControl<CopyPasteTextBox> ("txtSecret");
            _btnOK = this.FindControl<Button>("btnOK");
            _lblMessage = this.FindControl<TextBlock>("lblMessage");
            _txtSecret.Text = "";
            _btnOK.Click += (object sender, RoutedEventArgs e) =>
            {
                AllGood = true;
                this.Close(_txtSecret.Text);
            };

            this._secretType = secretType;
            switch (secretType)
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

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private  void InputSecretDialogView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!AllGood)
            {
                new Views.MessageBox(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                                                                   _loc.GetLocalizationValue("SecretDialogError"),
                                                                   MessageBoxSize.Small, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                                                                   .ShowDialog<MessagBoxDialogResult>(_mainWindow);
                e.Cancel = true;
            }
        }

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
