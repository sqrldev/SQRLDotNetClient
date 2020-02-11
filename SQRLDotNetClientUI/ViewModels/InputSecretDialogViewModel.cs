using Avalonia;
using SQRLDotNetClientUI.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    public class InputSecretDialogViewModel : ViewModelBase
    {
        public string Secret { get; set; }

        public async void Done()
        {
            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                    $"Error", $"The identity could not be decrypted using the given password! Please try again!",
                    MessageBox.Avalonia.Enums.ButtonEnum.Ok,
                    MessageBox.Avalonia.Enums.Icon.Error);

            await messageBoxStandardWindow.ShowDialog(AvaloniaLocator.Current.GetService<InputSecretDialogView>());
        }
    }
}
