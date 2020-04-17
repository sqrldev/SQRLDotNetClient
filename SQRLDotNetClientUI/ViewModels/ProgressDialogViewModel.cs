using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// A view model representing the app's "progress dialog" screen.
    /// </summary>
    public class ProgressDialogViewModel: ViewModelBase
    {
        /// <summary>
        /// Gets or sets a list of <c>Progress</c> objects being tracked by the progress dialog.
        /// </summary>
        public List<Progress<KeyValuePair<int, string>>> ProgressList { get; set; }

        /// <summary>
        /// Gets or sets the view model from which the progress dialog was called.
        /// This view model will be returned to after the progress dialog closes.
        /// </summary>
        public ViewModelBase Parent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether progress items which are finished
        /// should be hidden from the dialog or not.
        /// </summary>
        public bool HideFinishedItems { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether progress items that have not yet
        /// received any progress events should be hidden or not.
        /// </summary>
        public bool HideEnqueuedItems { get; set; }


        /// <summary>
        /// Creates a new <c>ProgressDialogViewModel</c> instance.
        /// </summary>
        private ProgressDialogViewModel()
        {
            
        }

        /// <summary>
        /// Creates a new <c>ProgressDialogViewModel</c> instance.
        /// </summary>
        /// <param name="parent">The view model calling the progress dialog.</param>
        /// <param name="hideFinishedItems">Specifies whether progress items which are finished
        /// should be hidden from the dialog or not.</param>
        /// <param name="hideEnqueuedItems">Specifies whether progress items that have not yet
        /// received any progress events should be hidden or not.</param>
        private ProgressDialogViewModel(ViewModelBase parent, bool hideFinishedItems = true, bool hideEnqueuedItems = true) : this()
        {
            this.Parent = parent;
            this.Title = this.Parent.Title;
            this.HideFinishedItems = hideFinishedItems;
            this.HideEnqueuedItems = hideEnqueuedItems;
        }

        /// <summary>
        /// Creates a new <c>ProgressDialogViewModel</c> instance.
        /// </summary>
        /// <param name="progressList">A list of <c>Progress</c> objects to be tracked by the progress dialog.</param>
        /// <param name="parent">The view model calling the progress dialog.</param>
        /// <param name="hideFinishedItems">Specifies whether progress items which are finished
        /// should be hidden from the dialog or not.</param>
        /// <param name="hideEnqueuedItems">Specifies whether progress items that have not yet
        /// received any progress events should be hidden or not.</param>
        public ProgressDialogViewModel(List<Progress<KeyValuePair<int, string>>> progressList, ViewModelBase parent, 
            bool HideFinishedItems = true, bool HideEnqueuedItems = true) : this(parent, HideFinishedItems, HideEnqueuedItems)
        {
            this.ProgressList = progressList;

        }

        /// <summary>
        /// Creates a new <c>ProgressDialogViewModel</c> instance.
        /// </summary>
        /// <param name="progress">A <c>Progress</c> object to be tracked by the progress dialog.</param>
        /// <param name="parent">The view model calling the progress dialog.</param>
        /// <param name="hideFinishedItems">Specifies whether progress items which are finished
        /// should be hidden from the dialog or not.</param>
        /// <param name="hideEnqueuedItems">Specifies whether progress items that have not yet
        /// received any progress events should be hidden or not.</param>
        public ProgressDialogViewModel(Progress<KeyValuePair<int, string>> progress, ViewModelBase parent, 
            bool HideFinishedItems = false, bool HideEnqueuedItems = false) : this (parent, HideFinishedItems, HideEnqueuedItems)
        {
            this.ProgressList = new List<Progress<KeyValuePair<int, string>>>();
            this.ProgressList.Add(progress);
        }

        /// <summary>
        /// Sets the current progress dialog as the foreground <c>UserControl</c>.
        /// </summary>
        public void ShowDialog()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = this;
        }


        /// <summary>
        /// Sets the parent of the progress dialog as the foreground <c>UserControl</c>.
        /// </summary>
        public void Close()
        {
            if(((MainWindowViewModel)_mainWindow.DataContext).Content==this)
                ((MainWindowViewModel)_mainWindow.DataContext).Content = this.Parent;
        }
    }
}
