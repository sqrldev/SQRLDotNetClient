using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System;
using System.ComponentModel;

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
            _btnOK.Click += _btnOK_Click;

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void _btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close(_txtSecret.Text);
        }

        private void _txtSecret_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;

            RoutedEventArgs click = new RoutedEventArgs
            {
                Source = _txtSecret,
                RoutedEvent = Button.ClickEvent
            };

            _btnOK.RaiseEvent(click);
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
