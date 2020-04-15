using System.Threading;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// Represents a view model for the <c>SelectIdentityView</c> screen.
    /// </summary>
    public class SelectIdentityViewModel : ViewModelBase
    {
        private AutoResetEvent _identitySelected = null;
        private ViewModelBase _parent = null;

        public SelectIdentityViewModel()
        {
            _identitySelected = new AutoResetEvent(false);
        }

        /// <summary>
        /// Handles the event of an identity being selected.
        /// </summary>
        public void OnIdentitySelected(string identityUniqueId)
        {
            _identityManager.SetCurrentIdentity(identityUniqueId);

            // Signal the auto reset event that we're done
            _identitySelected.Set();
        }

        public async void ShowDialog(ViewModelBase Parent)
        {
            this._parent = Parent;
            
            // Set the content of the main window to the select identity screen
            ((MainWindowViewModel)_mainWindow.DataContext).Content = this;
            
            // Wait for an identity to get selected
            await Task.Run(() => _identitySelected.WaitOne());

            // Set the content of the main window back to where it was before
            ((MainWindowViewModel)_mainWindow.DataContext).Content = _parent;
        }
    }
}
