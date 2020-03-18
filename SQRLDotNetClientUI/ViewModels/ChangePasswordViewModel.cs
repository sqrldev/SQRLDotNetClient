using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;

namespace SQRLDotNetClientUI.ViewModels
{
    class ChangePasswordViewModel : ViewModelBase
    {
        private static IBrush BRUSH_POOR = new SolidColorBrush(new Color(0xFF, 0xF0, 0x80, 0x80));
        private static IBrush BRUSH_MEDIUM = new SolidColorBrush(new Color(0xFF, 0xFF, 0xFA, 0xCD));
        private static IBrush BRUSH_GOOD = new SolidColorBrush(new Color(0xFF, 0x32, 0xCD, 0x32));

        private PasswordStrengthMeter _pwdStrengthMeter = new PasswordStrengthMeter();

        public ChangePasswordViewModel()
        {
            this.Title = _loc.GetLocalizationValue("ChangePasswordDialogTitle");
            if (_identityManager.CurrentIdentity != null) 
                this.Title += " (" + _identityManager.CurrentIdentity.IdentityName + ")";

            this.PasswordStrength = 0;
            _pwdStrengthMeter.ScoreUpdated += PaswordStrengthScoreUpdated;

            this.WhenAnyValue(x => x.NewPassword).Subscribe(x =>
            {
                _pwdStrengthMeter.Update(x);
                CheckPasswordVerification();
            });
            this.WhenAnyValue(x => x.NewPasswordVerify).Subscribe(x => CheckPasswordVerification());
        }

        /// <summary>
        /// Gets called if the <c>PasswordStrengthMeter</c> has calculated a new
        /// password strength rating.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PaswordStrengthScoreUpdated(object sender, ScoreUpdatedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                string ratingText = _loc.GetLocalizationValue("PasswordStrength") + ": ";

                switch (e.Score.Rating)
                {
                    case PasswordRating.POOR:
                        ratingText += _loc.GetLocalizationValue("PasswordStrengthRatingPoor");
                        this.PasswordRatingColor = BRUSH_POOR;
                        break;

                    case PasswordRating.MEDIUM:
                        ratingText += _loc.GetLocalizationValue("PasswordStrengthRatingMedium");
                        this.PasswordRatingColor = BRUSH_MEDIUM ;
                        break;

                    case PasswordRating.GOOD:
                        ratingText += _loc.GetLocalizationValue("PasswordStrengthRatingGood");
                        this.PasswordRatingColor = BRUSH_GOOD;
                        break;
                }

                this.UppercaseIndicatorColor = e.Score.UppercaseUsed ? BRUSH_GOOD : BRUSH_POOR;
                this.LowercaseIndicatorColor = e.Score.LowercaseUsed ? BRUSH_GOOD : BRUSH_POOR;
                this.DigitsIndicatorColor = e.Score.DigitsUsed ? BRUSH_GOOD : BRUSH_POOR;
                this.SymbolsIndicatorColor = e.Score.SymbolsUsed ? BRUSH_GOOD : BRUSH_POOR;

                this.PasswordStrengthRating = ratingText;
                this.PasswordStrength = (double)e.Score.StrengthPoints;
            });

            
        }

        /// <summary>
        /// Checks if the password verification matches the new password
        /// and enables/disables UI controls accordingly.
        /// </summary>
        private void CheckPasswordVerification()
        {
            if (!string.IsNullOrEmpty(this.NewPassword) && this.NewPassword == this.NewPasswordVerify)
            {
                this.CanSave = true;
            }
            else this.CanSave = false;
        }

        /// <summary>
        /// Closes the current view and displays the main screen.
        /// </summary>
        public void Close()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
        }

        /// <summary>
        /// Decrypts the current identity's block 1 using the old password
        /// and tries to re-encrypt it using the new password.
        /// </summary>
        public async void SetNewPassword()
        {
            CanSave = false;

            var progress = new Progress<KeyValuePair<int, string>>();
            var progressDialog = new ProgressDialog(progress);
            progressDialog.HideFinishedItems = false;
            progressDialog.ShowDialog(_mainWindow);

            var block1Keys = await SQRL.DecryptBlock1(_identityManager.CurrentIdentity, 
                this.Password, progress);

            if (!block1Keys.DecryptionSucceeded)
            {
                progressDialog.Close();

                await new MessageBox(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                    _loc.GetLocalizationValue("BadPasswordError"),
                    MessageBoxSize.Small, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                    .ShowDialog<MessagBoxDialogResult>(_mainWindow);

                CanSave = true;
                return;
            }

            // Decryption succeeded, let's go ahead
            var currentId = _identityManager.CurrentIdentity;
            var idCopy = _identityManager.CurrentIdentity.Clone();

            await SQRL.GenerateIdentityBlock1(block1Keys.Imk, block1Keys.Ilk, this.NewPassword, currentId, progress, (int)currentId.Block1.PwdVerifySeconds);
            
            progressDialog.Close();

            // Write the changes back to the db
            _identityManager.UpdateCurrentIdentity();

            CanSave = true;
            Close();
        }

        private bool _canSave = true;
        public bool CanSave
        {
            get => _canSave;
            set => this.RaiseAndSetIfChanged(ref _canSave, value);
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        private string _newPassword = "";
        public string NewPassword
        {
            get => _newPassword;
            set => this.RaiseAndSetIfChanged(ref _newPassword, value);
        }

        private string _newPasswordVerify = "";
        public string NewPasswordVerify
        {
            get => _newPasswordVerify;
            set => this.RaiseAndSetIfChanged(ref _newPasswordVerify, value);
        }

        private double _passwordStrength = 0;
        public double PasswordStrength
        {
            get => _passwordStrength;
            set => this.RaiseAndSetIfChanged(ref _passwordStrength, value);
        }

        private IBrush _passwordRatingColor = Brushes.Crimson;
        public IBrush PasswordRatingColor
        {
            get => _passwordRatingColor;
            set => this.RaiseAndSetIfChanged(ref _passwordRatingColor, value);
        }

        private IBrush _uppercaseIndicatorColor = Brushes.Crimson;
        public IBrush UppercaseIndicatorColor
        {
            get => _uppercaseIndicatorColor;
            set => this.RaiseAndSetIfChanged(ref _uppercaseIndicatorColor, value);
        }

        private IBrush _lowercaseIndicatorColor = Brushes.Crimson;
        public IBrush LowercaseIndicatorColor
        {
            get => _lowercaseIndicatorColor;
            set => this.RaiseAndSetIfChanged(ref _lowercaseIndicatorColor, value);
        }

        private IBrush _digitsIndicatorColor = Brushes.Crimson;
        public IBrush DigitsIndicatorColor
        {
            get => _digitsIndicatorColor;
            set => this.RaiseAndSetIfChanged(ref _digitsIndicatorColor, value);
        }

        private IBrush _symbolsIndicatorColor = Brushes.Crimson;
        public IBrush SymbolsIndicatorColor
        {
            get => _symbolsIndicatorColor;
            set => this.RaiseAndSetIfChanged(ref _symbolsIndicatorColor, value);
        }

        private string _passwordStrengthRating = "";
        public string PasswordStrengthRating
        {
            get => _passwordStrengthRating;
            set => this.RaiseAndSetIfChanged(ref _passwordStrengthRating, value);
        }

        private double _passwordStrengthMax = PasswordStrengthMeter.STRENGTH_POINTS_MIN_GOOD;
        public double PasswordStrengthMax
        {
            get => _passwordStrengthMax;
            set => this.RaiseAndSetIfChanged(ref _passwordStrengthMax, value);
        }
    }
}
