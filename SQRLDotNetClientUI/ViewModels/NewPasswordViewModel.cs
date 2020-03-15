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
    class NewPasswordViewModel : ViewModelBase
    {
        private PasswordStrengthMeter _pwdStrengthMeter = new PasswordStrengthMeter();

        private bool _canSave = true;
        public bool CanSave
        {
            get => _canSave;
            set => this.RaiseAndSetIfChanged(ref _canSave, value);
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

        private string _progressText = string.Empty;
        public string ProgressText
        {
            get => _progressText;
            set => this.RaiseAndSetIfChanged(ref _progressText, value);
        }

        public NewPasswordViewModel()
        {
            this.Title = _loc.GetLocalizationValue("ChangePasswordDialogTitle");
            if (_identityManager.CurrentIdentity != null) 
                this.Title += " (" + _identityManager.CurrentIdentity.IdentityName + ")";

            this.PasswordStrength = 0;
            _pwdStrengthMeter.ScoreUpdated += PaswordStrengthScoreUpdated;

            this.WhenAnyValue(x => x.NewPassword).Subscribe(x => _pwdStrengthMeter.Update(x));
            this.WhenAnyValue(x => x.NewPasswordVerify).Subscribe(x => PasswordVerifyChanged(x));
        }

        private void PaswordStrengthScoreUpdated(object sender, ScoreUpdatedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                string ratingText = _loc.GetLocalizationValue("PasswordStrength") + ": ";

                switch (e.Score.Rating)
                {
                    case PasswordRating.POOR:
                        ratingText += _loc.GetLocalizationValue("PasswordStrengthRatingPoor");
                        this.PasswordRatingColor = Brushes.Crimson;
                        break;

                    case PasswordRating.MEDIUM:
                        ratingText += _loc.GetLocalizationValue("PasswordStrengthRatingMedium");
                        this.PasswordRatingColor = Brushes.LightGoldenrodYellow;
                        break;

                    case PasswordRating.GOOD:
                        ratingText += _loc.GetLocalizationValue("PasswordStrengthRatingGood");
                        this.PasswordRatingColor = Brushes.LightGreen;
                        break;
                }

                this.PasswordStrengthRating = ratingText;
                this.PasswordStrength = (double)e.Score.StrengthPoints;
            });

            
        }

        private void PasswordVerifyChanged(string newPassword)
        {
            
        }

        public void Close()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
        }

        public async void SetNewPassword()
        {
            CanSave = false;

            var progress = new Progress<KeyValuePair<int, string>>(p =>
            {
                this.PasswordStrength = (double)p.Key;
                this.ProgressText = p.Value + p.Key;
            });

            var block1Keys = await SQRL.DecryptBlock1(_identityManager.CurrentIdentity, 
                this.NewPassword, progress);

            if (!block1Keys.DecryptionSucceeded)
            {

                await new Views.MessageBox(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                                           _loc.GetLocalizationValue("BadPasswordError"),
                                           MessageBoxSize.Small, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                                           .ShowDialog<MessagBoxDialogResult>(_mainWindow);
                ProgressText = "";
                PasswordStrength = 0;
                CanSave = true;
                return;
            }

            //SQRLIdentity id = await SQRL.GenerateIdentityBlock1(block1Keys.Imk, block1Keys.Ilk,
            //    password, IdentityCopy, progress, IdentityCopy.Block1.PwdVerifySeconds);

            //// Swap out the old type 1 block with the updated one
            //// TODO: We should probably make sure that this is an atomic operation
            //Identity.Blocks.Remove(Identity.Block1);
            //Identity.Blocks.Insert(0, id.Block1);

            //// Finally, update the identity in the db
            //_identityManager.UpdateIdentity(Identity);

            CanSave = true;
            Close();
        }
    }
}
