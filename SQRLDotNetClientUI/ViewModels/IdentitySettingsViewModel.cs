using Avalonia;
using Avalonia.Interactivity;
using ReactiveUI;
using SQRLDotNetClientUI.Views;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    class IdentitySettingsViewModel : ViewModelBase
    {
        public SQRL SqrlInstance { get; set; }
        public SQRLIdentity Identity { get; set; }
        public SQRLIdentity IdentityCopy { get; set; }

        private bool _canSave = true;
        public bool CanSave { get => _canSave; set => this.RaiseAndSetIfChanged(ref _canSave, value); }

        private int _ProgressPercentage = 0;
        public int ProgressPercentage { get => _ProgressPercentage; set => this.RaiseAndSetIfChanged(ref _ProgressPercentage, value); }
        public int ProgressMax { get; set; } = 100;
        public string GenerationStep { get; set; }

        public IdentitySettingsViewModel() { }

        public IdentitySettingsViewModel(SQRL sqrlInstance, SQRLIdentity identity)
        {
            this.Title = "SQRL Client - Identity Settings";
            this.SqrlInstance = sqrlInstance;
            this.Identity = identity;
            this.IdentityCopy = identity.Clone();

            if (identity != null) this.Title += " (" + identity.IdentityName + ")";
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

            string password = "test12345678";

            var progress = new Progress<KeyValuePair<int, string>>(percent =>
            {
                this.ProgressPercentage = (int)percent.Key / 2;
                this.GenerationStep = percent.Value + percent.Key;
            });

            (bool ok, byte[] imk, byte[] ilk) = await SqrlInstance.DecryptBlock1(Identity, password, progress);

            if (!ok)
            {
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                    $"Error", $"The identity could not be decrypted using the given password! Please try again!", 
                    MessageBox.Avalonia.Enums.ButtonEnum.Ok, 
                    MessageBox.Avalonia.Enums.Icon.Error);

                await messageBoxStandardWindow.ShowDialog(AvaloniaLocator.Current.GetService<MainWindow>());

                CanSave = true;
                return;
            }

            SQRLIdentity id = await SqrlInstance.GenerateIdentityBlock1(
                imk, ilk, password, IdentityCopy, progress, IdentityCopy.Block1.PwdVerifySeconds);

            // Swap out the old type 1 block with the updated one
            //TODO: We should probably make sure that this is an atomic operation
            Identity.Blocks.Remove(Identity.Block1);
            Identity.Blocks.Insert(0, id.Block1);

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

            if (Identity.Block1.OptionFlags.CheckForUpdates != IdentityCopy.Block1.OptionFlags.CheckForUpdates) return true;
            if (Identity.Block1.OptionFlags.ClearQuickPassOnIdle != IdentityCopy.Block1.OptionFlags.ClearQuickPassOnIdle) return true;
            if (Identity.Block1.OptionFlags.ClearQuickPassOnSleep != IdentityCopy.Block1.OptionFlags.ClearQuickPassOnSleep) return true;
            if (Identity.Block1.OptionFlags.ClearQuickPassOnSwitchingUser != IdentityCopy.Block1.OptionFlags.ClearQuickPassOnSwitchingUser) return true;
            if (Identity.Block1.OptionFlags.EnableMITMAttackWarning != IdentityCopy.Block1.OptionFlags.EnableMITMAttackWarning) return true;
            if (Identity.Block1.OptionFlags.EnableNoCPSWarning != IdentityCopy.Block1.OptionFlags.EnableNoCPSWarning) return true;
            if (Identity.Block1.OptionFlags.RequestNoSQRLBypass != IdentityCopy.Block1.OptionFlags.RequestNoSQRLBypass) return true;
            if (Identity.Block1.OptionFlags.RequestSQRLOnlyLogin != IdentityCopy.Block1.OptionFlags.RequestSQRLOnlyLogin) return true;
            if (Identity.Block1.OptionFlags.UpdateAutonomously != IdentityCopy.Block1.OptionFlags.UpdateAutonomously) return true;

            return false;
        }
    }
}
