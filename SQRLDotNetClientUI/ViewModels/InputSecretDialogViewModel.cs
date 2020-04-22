using ReactiveUI;
using SQRLDotNetClientUI.Views;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// A view model for the app's screen to enter a user secret
    /// (password or rescue code).
    /// </summary>
    public class InputSecretDialogViewModel: ViewModelBase
    {
        private AutoResetEvent _dialogClosed = null;
        private string _secret = "";

        /// <summary>
        /// Gets or sets the type of secret that the user should be asked to enter.
        /// </summary>
        public SecretType SecretType { get; set; }

        /// <summary>
        /// Gets or sets the actual secret entered by the user.
        /// </summary>
        public string Secret {
            get => _secret;
            set => this.RaiseAndSetIfChanged(ref _secret, value);
        }

        /// <summary>
        /// Gets or sets the view model that the Input Secret Dialog was called from and
        /// that should be shown again when the dialog gets closed.
        /// </summary>
        public ViewModelBase Parent { get; set; }

        /// <summary>
        /// Creates a new <c>InputSecretDialogViewModel</c> instance, specifying the
        /// type of secret that the user should be asked to enter within <paramref name="secretType"/>.
        /// </summary>
        /// <param name="secretType">The type of secret that the user should be asked to enter.</param>
        public InputSecretDialogViewModel(SecretType secretType=SecretType.Password)
        {
            this.SecretType = secretType;
            _dialogClosed = new AutoResetEvent(false);
        }

        /// <summary>
        /// This event handler gets called when the dialog's "OK" button is clicked.
        /// </summary>
        public void Ok()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = Parent;
            _dialogClosed.Set();
        }

        /// <summary>
        /// This method sets the current view to the Input Secret Dialog then waits for the 
        /// "OK" button to be clicked before returning.
        /// </summary>
        /// <param name="parent">The view model that the Input Secret Dialog was called from
        /// and that should be shown again when the dialog gets closed.</param>
        /// <param name="title">An optional title for the dialog window.</param>
        public async Task<bool> ShowDialog(ViewModelBase parent, string title = "")
        {
            this.Parent = parent;
            this.Title = string.IsNullOrEmpty(title) ? parent.Title : title;

            ((MainWindowViewModel)_mainWindow.DataContext).Content = this;

            return await Task.Run(() =>
            {
                _dialogClosed.WaitOne();
                return true;
            });
        }
    }
}
