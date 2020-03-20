using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// ViewModel used for re-keying the current Identity Key
    /// </summary>
    public class ReKeyViewModel : ViewModelBase
    {
        private static IBrush BRUSH_POOR = new SolidColorBrush(new Color(0xFF, 0xF0, 0x80, 0x80));
        private static IBrush BRUSH_MEDIUM = new SolidColorBrush(new Color(0xFF, 0xFF, 0xFA, 0xCD));
        private static IBrush BRUSH_GOOD = new SolidColorBrush(new Color(0xFF, 0x32, 0xCD, 0x32));
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
        private PasswordStrengthMeter _pwdStrengthMeter = new PasswordStrengthMeter();
        public ReKeyViewModel()
        {
            Init();
        }
        private string _ReKeyIdentityExplanation;
        public string ReKeyIdentityExplanation
        {
            get => _ReKeyIdentityExplanation;
            set => this.RaiseAndSetIfChanged(ref _ReKeyIdentityExplanation, value);
        }

        /// <summary>
        /// Called from Constructor(s) to initialize various things
        /// </summary>
        public void Init()
        {
            if (_loc != null)
            {
                this.Title = string.Format(_loc.GetLocalizationValue("ReKeyIdentityTitle"), _identityManager.CurrentIdentity.IdentityName);
                
            }
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
                        this.PasswordRatingColor = BRUSH_MEDIUM;
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

        public void Cancel()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
        }

        /// <summary>
        /// Runs the Re-Key Logic
        /// </summary>
        public async void Next()
        {
            //Label used for retrying the rescue code if you type it wrong the first time
            RetryRescueCode:

            //Dialog Box used to capture the user's current Rescue Code
            InputSecretDialogView rescueCodeDlg = new InputSecretDialogView(SecretType.RescueCode);
            rescueCodeDlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            string rescueCode = await rescueCodeDlg.ShowDialog<string>(
                _mainWindow);

            //List of Progress Reporters to be used during the re-key process (progress bar magic)
            var  progressList = new List<Progress<KeyValuePair<int, string>>>(){ new Progress<KeyValuePair<int, string>>(),new Progress<KeyValuePair<int, string>>() };

            //Progress Dialog will show our "Progress" as the Identity is Decrypted, and Re-Encrypted for Rekey
            var progressDialog = new ProgressDialog(progressList);
            progressDialog.HideFinishedItems = true;
            progressDialog.HideEnqueuedItems = true;
            _ = progressDialog.ShowDialog(_mainWindow);

            //Actually do the Re-Key Work
            var result = await SQRL.RekeyIdentity(_identityManager.CurrentIdentity, SQRL.CleanUpRescueCode(rescueCode), NewPassword, progressList[0],progressList[1]);
            if(!result.Success)
            {
                progressDialog.Close();
                //Fail bad rescue code (something went wrong...) try again?
                var answer = await new Views.MessageBox(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                                                                   _loc.GetLocalizationValue("InvalidRescueCodeMessage"),
                                                                   MessageBoxSize.Small, MessageBoxButtons.YesNo, MessageBoxIcons.ERROR)
                                                                   .ShowDialog<MessagBoxDialogResult>(_mainWindow);
                if (answer == MessagBoxDialogResult.YES)
                {
                    goto RetryRescueCode; //Go back up and re-do it all, this time with passion!
                }
            }
            else if(result.Success) //All Good
            {
                progressDialog.Close();

                //This label is used to re-share the new rescue code if it was copied incorrectly.
                CopiedWrong:
                //Message Box which displays the new Rescue Code to the user
                await new Views.MessageBox(_loc.GetLocalizationValue("IdentityReKeyNewCode"),
                                                                   string.Format(_loc.GetLocalizationValue("IdentityReKeyMessage"),SQRL.FormatRescueCodeForDisplay((result.NewRescueCode))),
                                                                   MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.OK)
                                                                   .ShowDialog<MessagBoxDialogResult>(_mainWindow);

                //Ask the user to re-type their New Rescue Code to verify that they copied it correctly.
                rescueCodeDlg = new InputSecretDialogView(SecretType.RescueCode)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                rescueCode = await rescueCodeDlg.ShowDialog<string>(
                    _mainWindow);
                
                //New progress dialog for the verification step
                progressDialog = new ProgressDialog(progressList[0]);
                progressDialog.HideFinishedItems = false;
                _ = progressDialog.ShowDialog(_mainWindow);

                //Decrypt Block 2 to verify they copied their rescue code correctly.
                var block2Results = await SQRL.DecryptBlock2(result.RekeyedIdentity, rescueCode, progressList[0]);
                if (block2Results.DecryptionSucceeded) //All Good , All Done
                {
                    progressDialog.Close();
                    _identityManager.DeleteCurrentIdentity();
                    _identityManager.ImportIdentity(result.RekeyedIdentity, true);
                    ((MainWindowViewModel)_mainWindow.DataContext).Content = ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
                }
                else //Fail bad rescue code... try again?
                {
                    progressDialog.Close();
                    var answer = await new Views.MessageBox(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                                                                   _loc.GetLocalizationValue("InvalidRescueCodeMessage"),
                                                                   MessageBoxSize.Small, MessageBoxButtons.YesNo, MessageBoxIcons.ERROR)
                                                                   .ShowDialog<MessagBoxDialogResult>(_mainWindow);
                    if (answer == MessagBoxDialogResult.YES)
                    {
                        goto CopiedWrong; //Try Again
                    }
                    else //Abort the whole thing
                    {
                        _=await new Views.MessageBox(_loc.GetLocalizationValue("ErrorTitleGeneric"),
                                                                   _loc.GetLocalizationValue("IdentityReKeyFailed"),
                                                                   MessageBoxSize.Medium, MessageBoxButtons.OK, MessageBoxIcons.ERROR)
                                                                   .ShowDialog<MessagBoxDialogResult>(_mainWindow);
                    }
                }
                
            }
             ((MainWindowViewModel)_mainWindow.DataContext).Content = ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
        }
    }


}
