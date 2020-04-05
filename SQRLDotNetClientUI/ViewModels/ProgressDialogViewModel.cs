using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    public class ProgressDialogViewModel: ViewModelBase
    {
        public List<Progress<KeyValuePair<int, string>>> ProgressList { get; set; }
        public ViewModelBase Parent { get; set; }

        public bool HideFinishedItems { get; set; }
        public bool HideEnqueuedItems { get; set; }

        

        private ProgressDialogViewModel()
        {
            
        }
        private ProgressDialogViewModel(ViewModelBase model, bool HideFinishedItems = true, bool HideEnqueuedItems = true) :this()
        {
            this.Parent = model;
            this.Title = this.Parent.Title;
            this.HideFinishedItems = HideFinishedItems;
            this.HideEnqueuedItems = HideEnqueuedItems;
        }

        public ProgressDialogViewModel(List<Progress<KeyValuePair<int, string>>> progressList, ViewModelBase model, bool HideFinishedItems = true, bool HideEnqueuedItems = true) :this(model, HideFinishedItems, HideEnqueuedItems)
        {
            this.ProgressList = progressList;

        }

        public ProgressDialogViewModel(Progress<KeyValuePair<int, string>> progress, ViewModelBase model, bool HideFinishedItems = false, bool HideEnqueuedItems = false) : this (model, HideFinishedItems, HideEnqueuedItems)
        {
            this.ProgressList = new List<Progress<KeyValuePair<int, string>>>();
            this.ProgressList.Add(progress);
        }

        /// <summary>
        /// Sets the current progress dialog as the forground User Control
        /// </summary>
        public void ShowDialog()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = this;
        }


        /// <summary>
        /// Sets the Parent of the Progress Dialog back to the Foreground control.
        /// </summary>
        public void Close()
        {
            if(((MainWindowViewModel)_mainWindow.DataContext).Content==this)
                ((MainWindowViewModel)_mainWindow.DataContext).Content = this.Parent;
        }
    }
}
