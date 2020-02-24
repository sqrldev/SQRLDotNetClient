using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using SQRLDotNetClientUI.AvaloniaExtensions;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;

namespace SQRLDotNetClientUI.ViewModels
{
    class IdentitySettingsViewModel : ViewModelBase
    {
        private IdentityManager _identityManager = IdentityManager.Instance;
        private LocalizationExtension loc = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService;
        public SQRL SqrlInstance { get; set; }
        public SQRLIdentity Identity { get; set; }
        public SQRLIdentity IdentityCopy { get; set; }

        private bool _canSave = true;
        public bool CanSave { get => _canSave; set => this.RaiseAndSetIfChanged(ref _canSave, value); }

        private double _ProgressPercentage = 0;
        public double ProgressPercentage { get => _ProgressPercentage; set => this.RaiseAndSetIfChanged(ref _ProgressPercentage, value); }

        public double ProgressMax { get; set; } = 100;

        private string _progressText = string.Empty;
        public string ProgressText { get => _progressText; set => this.RaiseAndSetIfChanged(ref _progressText, value); }

        public IdentitySettingsViewModel() { }

        public IdentitySettingsViewModel(SQRL sqrlInstance)
        {
            this.Title = loc.GetLocalizationValue("IdentitySettingsDialogTitle");
            this.SqrlInstance = sqrlInstance;
            this.Identity = _identityManager.CurrentIdentity;
            this.IdentityCopy = this.Identity.Clone();

            if (this.Identity != null) this.Title += " (" + this.Identity.IdentityName + ")";
        }

        public void Close()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content =
                ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).MainMenu;
        }

        public async void Save()
        {
            CanSave = false;

            if (!HasChanges())
            {
                Close();
                CanSave = true;
                return;
            }

            
            InputSecretDialogView passwordDlg = new InputSecretDialogView(SecretType.Password);
            passwordDlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            string password = await passwordDlg.ShowDialog<string>(
                AvaloniaLocator.Current.GetService<MainWindow>());

            if (password == null)
            {
                CanSave = true;
                return;
            }

            var progress = new Progress<KeyValuePair<int, string>>(p =>
            {
                this.ProgressPercentage = (double)p.Key;
                this.ProgressText = p.Value + p.Key;
            });

            (bool ok, byte[] imk, byte[] ilk) = await SqrlInstance.DecryptBlock1(Identity, password, progress);

            if (!ok)
            {
                var msgBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                    loc.GetLocalizationValue("ErrorTitleGeneric"),
                    loc.GetLocalizationValue("BadPasswordError"),
                    MessageBox.Avalonia.Enums.ButtonEnum.Ok, 
                    MessageBox.Avalonia.Enums.Icon.Error);

                await msgBox.ShowDialog(AvaloniaLocator.Current.GetService<MainWindow>());

                ProgressText = "";
                ProgressPercentage = 0;
                CanSave = true;
                return;
            }

            SQRLIdentity id = await SqrlInstance.GenerateIdentityBlock1(
                imk, ilk, password, IdentityCopy, progress, IdentityCopy.Block1.PwdVerifySeconds);

            // Swap out the old type 1 block with the updated one
            // TODO: We should probably make sure that this is an atomic operation
            Identity.Blocks.Remove(Identity.Block1);
            Identity.Blocks.Insert(0, id.Block1);

            // Finally, update the identity in the db
            _identityManager.UpdateIdentity(Identity);

            CanSave = true;
            Close();
        }

        /// <summary>
        /// Returns <c>true</c> if any of the identity settings were changed by the 
        /// user are and those changes have not been applied yet, or <c>false</c> otherwise.
        /// </summary>
        public bool HasChanges()
        {
            if (Identity.Block1.HintLength != IdentityCopy.Block1.HintLength) return true;
            if (Identity.Block1.PwdTimeoutMins != IdentityCopy.Block1.PwdTimeoutMins) return true;
            if (Identity.Block1.PwdVerifySeconds != IdentityCopy.Block1.PwdVerifySeconds) return true;
            if (Identity.Block1.OptionFlags.FlagsValue != IdentityCopy.Block1.OptionFlags.FlagsValue) return true;

            return false;
        }
    }
}
