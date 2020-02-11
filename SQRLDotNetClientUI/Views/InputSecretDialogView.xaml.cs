using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace SQRLDotNetClientUI.Views
{
    public class InputSecretDialogView : Window
    {

        private TextBox _txtSecret = null;
        private Button _btnOK = null;

        public InputSecretDialogView()
        {
            this.InitializeComponent();
            _txtSecret = this.FindControl<TextBox>("txtSecret");
            _btnOK = this.FindControl<Button>("btnOK");


            _txtSecret.KeyUp += _txtSecret_KeyUp;
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void _txtSecret_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;

            RoutedEventArgs click = new RoutedEventArgs
            {
                RoutedEvent = Button.ClickEvent
            };

            this.RaiseEvent(click);
            e.Handled = true;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            Application.Current.FocusManager.Focus(_txtSecret);
        }
    }
}
