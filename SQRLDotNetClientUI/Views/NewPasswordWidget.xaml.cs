using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using SQRLDotNetClientUI.AvaloniaExtensions;
using SQRLDotNetClientUI.Models;
using System.Reactive.Linq;
using System;
using Avalonia.Controls.Shapes;
using Avalonia.Data;

namespace SQRLDotNetClientUI.Views
{
    /// <summary>
    /// Represents a collection of UI controls for entering and verifying 
    /// a new password . It also displays a "password strength meter".
    /// </summary>
    public class NewPasswordWidget : UserControl
    {
        private static IBrush BRUSH_POOR = new SolidColorBrush(new Color(0xFF, 0xF0, 0x80, 0x80));
        private static IBrush BRUSH_MEDIUM = new SolidColorBrush(new Color(0xFF, 0xFF, 0xFA, 0xCD));
        private static IBrush BRUSH_GOOD = new SolidColorBrush(new Color(0xFF, 0x32, 0xCD, 0x32));

        private LocalizationExtension _loc = new LocalizationExtension();
        private PasswordStrengthMeter _pwdStrengthMeter = new PasswordStrengthMeter();
        private CopyPasteTextBox _txtNewPassword = null;
        private CopyPasteTextBox _txtNewPasswordVerify = null;
        private Panel _pnlPasswordRating = null;
        private ProgressBar _progressPwStrength = null;
        private TextBlock _lblPasswordStrength = null;
        private Ellipse _shapeUppercaseIndicator = null;
        private Ellipse _shapeLowercaseIndicator = null;
        private Ellipse _shapeDigigsIndicator = null;
        private Ellipse _shapeSymbolsIndicator = null;

        public static readonly AvaloniaProperty<bool> PasswordsMatchProperty =
            AvaloniaProperty.Register<NewPasswordWidget, bool>(nameof(PasswordsMatch), defaultValue: false, 
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly AvaloniaProperty<string> NewPasswordProperty =
            AvaloniaProperty.Register<NewPasswordWidget, string>(nameof(NewPassword),
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly AvaloniaProperty<string> NewPasswordVerificationProperty =
            AvaloniaProperty.Register<NewPasswordWidget, string>(nameof(NewPasswordVerification),
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Indicates whether the entered password and the password verification 
        /// are equal.
        /// </summary>
        public bool PasswordsMatch
        {
            get { return this.GetValue(PasswordsMatchProperty); }
            set { this.SetValue(PasswordsMatchProperty, value); }
        }

        /// <summary>
        /// The password entered by the user.
        /// </summary>
        public string NewPassword
        {
            get { return this.GetValue(NewPasswordProperty); }
            set { this.SetValue(NewPasswordProperty, value); }
        }

        /// <summary>
        /// The password verification entered by the user.
        /// </summary>
        public string NewPasswordVerification
        {
            get { return this.GetValue(NewPasswordVerificationProperty); }
            set { this.SetValue(NewPasswordVerificationProperty, value); }
        }

        public NewPasswordWidget()
        {
            this.InitializeComponent();

            _pwdStrengthMeter.ScoreUpdated += PasswordStrengthScoreUpdated;
            _txtNewPassword = this.FindControl<CopyPasteTextBox>("txtNewPassword");
            _txtNewPasswordVerify = this.FindControl<CopyPasteTextBox>("txtNewPasswordVerify");
            _pnlPasswordRating = this.FindControl<Panel>("pnlPasswordRating");
            _progressPwStrength = this.FindControl<ProgressBar>("progressPwStrength");
            _lblPasswordStrength = this.FindControl<TextBlock>("lblPasswordStrength");
            _shapeUppercaseIndicator = this.FindControl<Ellipse>("shapeUppercaseIndicator");
            _shapeLowercaseIndicator = this.FindControl<Ellipse>("shapeLowercaseIndicator");
            _shapeDigigsIndicator = this.FindControl<Ellipse>("shapeDigigsIndicator");
            _shapeSymbolsIndicator = this.FindControl<Ellipse>("shapeSymbolsIndicator");

            _txtNewPassword.GetObservable(CopyPasteTextBox.TextProperty).Subscribe(value =>
            {
                _pwdStrengthMeter.Update(value);
                this.NewPassword = value;
                CheckPasswordVerification();
            });

            _txtNewPasswordVerify.GetObservable(CopyPasteTextBox.TextProperty).Subscribe(value =>
            {
                this.NewPasswordVerification = value;
                CheckPasswordVerification();
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Checks if the password verification matches the new password
        /// and enables/disables UI controls accordingly.
        /// </summary>
        private void CheckPasswordVerification()
        {
            if (!string.IsNullOrEmpty(_txtNewPassword.Text) && 
                _txtNewPassword.Text == _txtNewPasswordVerify.Text)
            {
                this.PasswordsMatch = true;
            }
            else this.PasswordsMatch = false;
        }

        /// <summary>
        /// Gets called if the <c>PasswordStrengthMeter</c> has calculated a new
        /// password strength rating.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PasswordStrengthScoreUpdated(object sender, ScoreUpdatedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                string ratingText = _loc.GetLocalizationValue("PasswordStrength") + ": ";

                switch (e.Score.Rating)
                {
                    case PasswordRating.POOR:
                        ratingText += _loc.GetLocalizationValue("PasswordStrengthRatingPoor");
                        _pnlPasswordRating.Background = BRUSH_POOR;
                        break;

                    case PasswordRating.MEDIUM:
                        ratingText += _loc.GetLocalizationValue("PasswordStrengthRatingMedium");
                        _pnlPasswordRating.Background = BRUSH_MEDIUM;
                        break;

                    case PasswordRating.GOOD:
                        ratingText += _loc.GetLocalizationValue("PasswordStrengthRatingGood");
                        _pnlPasswordRating.Background = BRUSH_GOOD;
                        break;
                }

                _shapeUppercaseIndicator.Fill = e.Score.UppercaseUsed ? BRUSH_GOOD : BRUSH_POOR;
                _shapeLowercaseIndicator.Fill = e.Score.LowercaseUsed ? BRUSH_GOOD : BRUSH_POOR;
                _shapeDigigsIndicator.Fill = e.Score.DigitsUsed ? BRUSH_GOOD : BRUSH_POOR;
                _shapeSymbolsIndicator.Fill = e.Score.SymbolsUsed ? BRUSH_GOOD : BRUSH_POOR;

                _lblPasswordStrength.Text = ratingText;
                _progressPwStrength.Value = (double)e.Score.StrengthPoints;
            });
        }
    }
}
