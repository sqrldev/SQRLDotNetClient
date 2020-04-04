using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SQRLCommonUI.AvaloniaExtensions;
using SQRLDotNetClientUI.ViewModels;

namespace SQRLDotNetClientUI.Views
{
    public class AuthenticationView : UserControl
    {
        private CopyPasteTextBox _txtPassword = null;
        private CopyPasteTextBox _txtAltID = null;

        public AuthenticationView()
        {
            this.InitializeComponent();
            _txtPassword = this.FindControl<CopyPasteTextBox>("txtPassword");
            _txtAltID = this.FindControl<CopyPasteTextBox>("txtAltID");
            this.GotFocus += AuthenticationView_GotFocus;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void AuthenticationView_GotFocus(object sender, Avalonia.Input.GotFocusEventArgs e)
        {
            if (e?.Source is Button)
            {
                if (((Button)e.Source).Name == "btnIdentitySelector")
                {
                    _txtPassword.Focus();
                    e.Handled = true;
                }

                if (((Button)e.Source).Name == "btnAdvancedFunctions")
                {
                    _txtAltID.Focus();
                    e.Handled = true;
                }
            }
        }
    }
}
