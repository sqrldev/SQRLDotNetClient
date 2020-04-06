using ReactiveUI;
using SQRLDotNetClientUI.Views;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.ViewModels
{
    public class InputSecretDialogViewModel: ViewModelBase
    {
        public SecretType SecretType { get; set; }
        private BlockingCollection<bool> dialogClosed;
        private string _secret = "";
        public string Secret {
            get => _secret;
            set => this.RaiseAndSetIfChanged(ref _secret, value);
        }
    

        public ViewModelBase Parent { get; set; }
        public InputSecretDialogViewModel(SecretType secretType=SecretType.Password)
        {
            this.SecretType = secretType;
            dialogClosed = new BlockingCollection<bool>();
        }

        public void Ok()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content =Parent;
            dialogClosed.Add(true);
        }

        /// <summary>
        /// This method sets the current view to the Input Secret Dialog then waits for the OK butto to be clicked before returning
        /// uses a semaphore to accomplish this.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public async Task<bool> ShowDialog(ViewModelBase parent, string title = "")
        {
            this.Parent = parent;
            this.Title = string.IsNullOrEmpty(title)? parent.Title:title;
            ((MainWindowViewModel)_mainWindow.DataContext).Content = this;
            return await Task.Run(() =>
            {
                foreach (var x in dialogClosed.GetConsumingEnumerable())
                    return x;

                return false;
            });
        }
    }
}
