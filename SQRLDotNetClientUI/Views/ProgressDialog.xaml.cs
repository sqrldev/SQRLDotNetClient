using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using SQRLDotNetClientUI.AvaloniaExtensions;
using System;
using System.Collections.Generic;

namespace SQRLDotNetClientUI.Views
{
    /// <summary>
    /// A dialog window to display an arbitrary number of progess indicators.
    /// Progress items can be added through the constructor or passed in
    /// using the <c>AddProgressItem()</c> method.
    /// 
    /// The behaviour of the dialog can be adjusted using the <c>HideFinishedItems</c>
    /// and <c>HideEnqueuedItems</c> properties.
    /// </summary>
    public class ProgressDialog : Window
    {
        private LocalizationExtension _loc = AvaloniaLocator.Current.GetService<MainWindow>().LocalizationService;
        private Dictionary<Progress<KeyValuePair<int, string>>, ProgressItem> _progDict;
        private StackPanel _MainPanel;
        private StackPanel _DummyPanel;
        private int _count = 0;

        private bool _hideFinishedItems = true;
        /// <summary>
        /// If set to <c>true</c>, removes finished progress items from the dialog.
        /// </summary>
        public bool HideFinishedItems 
        {
            get => _hideFinishedItems;
            set
            {
                if (_hideFinishedItems != value)
                {
                    _hideFinishedItems = value;
                    foreach (var item in _progDict)
                    {
                        if (item.Value.ProgressBar.Value == 100)
                        {
                            item.Value.ProgressPanel.IsVisible = !value;
                        }
                    }
                }
            }
        }

        private bool _hideEnqueuedItems = true;
        /// <summary>
        /// If set to <c>true</c>, progress items will be hidden until the first
        /// <c>ProgressChanged</c> event arrives.
        /// </summary>
        public bool HideEnqueuedItems 
        { 
            get => _hideEnqueuedItems;
            set
            {
                if (_hideEnqueuedItems != value)
                {
                    _hideEnqueuedItems = value;
                    foreach (var item in _progDict)
                    {
                        if (item.Value.ProgressBar.Value == 0)
                        {
                            item.Value.ProgressPanel.IsVisible = !value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new <c>ProgressDialog</c> instance and sets up some required resources.
        /// </summary>
        public ProgressDialog()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            _progDict = new Dictionary<Progress<KeyValuePair<int, string>>, ProgressItem>();
            _MainPanel = this.FindControl<StackPanel>("MainPanel");
            _DummyPanel = this.FindControl<StackPanel>("DummyPanel");
        }

        /// <summary>
        /// Creates a new <c>ProgressDialog</c> instance and adds the provided progress items
        /// containted in <paramref name="progressList"/>.
        /// </summary>
        /// <param name="progressList">A list of progress items to track.</param>
        public ProgressDialog(List<Progress<KeyValuePair<int, string>>> progressList) : this()
        {
            foreach(var progress in progressList)
            {
                AddProgressItem(progress);
            }
        }

        /// <summary>
        /// Creates a new <c>ProgressDialog</c> instance and adds the provided progress item
        /// <paramref name="progress"/>.
        /// </summary>
        /// <param name="progressList">The progress item to track.</param>
        public ProgressDialog(Progress<KeyValuePair<int, string>> progress) : this()
        {
            AddProgressItem(progress);
        }

        /// <summary>
        /// Adds a progress item to the progress dialog.
        /// </summary>
        /// <param name="progress"></param>
        public void AddProgressItem(Progress<KeyValuePair<int, string>> progress)
        {
            _progDict.Add(progress, CreateProgressInfo(progress));
            if (_count == 0) _DummyPanel.IsVisible = false;
            _count++;
        }

        /// <summary>
        /// Destructs the object after doing some houskeeping.
        /// </summary>
        ~ProgressDialog()
        {
            // Unsubscribe from progress changes
            foreach (var progress in _progDict)
                progress.Key.ProgressChanged -= ProgressChanged;
        }

        private ProgressItem CreateProgressInfo(Progress<KeyValuePair<int, string>> progress)
        {
            var progressPanel = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 20, 0, 0),
                IsVisible = this.HideEnqueuedItems ? false : true
            };
            var textBlock = new TextBlock() { Text = _loc.GetLocalizationValue("ProgressDialogInQueue") };
            var progressBar = new ProgressBar() { IsIndeterminate = false, Minimum=0, Maximum=100 };

            progress.ProgressChanged += ProgressChanged;

            progressPanel.Children.Add(textBlock);
            progressPanel.Children.Add(progressBar);
            _MainPanel.Children.Add(progressPanel);

            ProgressItem progressItem = new ProgressItem
            {
                Progress = progress,
                ProgressPanel = progressPanel,
                TextBlock = textBlock,
                ProgressBar = progressBar
            };

            return progressItem;
        }

        private void ProgressChanged(object sender, KeyValuePair<int, string> e)
        {
            int progressPercentage = e.Key;
            string progressText = e.Value;

            Progress<KeyValuePair<int, string>> progress = (Progress<KeyValuePair<int, string>>)sender;

            if (!_progDict[progress].ProgressPanel.IsVisible)
                _progDict[progress].ProgressPanel.IsVisible = true;

            _progDict[progress].ProgressBar.Value = progressPercentage;
            _progDict[progress].TextBlock.Text = progressText;

            if (progressPercentage == 100 && this.HideFinishedItems)
                _progDict[progress].ProgressPanel.IsVisible = false;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    /// <summary>
    /// Represents a container for progress information used to tying 
    /// a <c>Progress<T></c> object to its affiliated UI controls.
    /// </summary>
    public class ProgressItem
    {
        public Progress<KeyValuePair<int, string>> Progress;
        public StackPanel ProgressPanel;
        public TextBlock TextBlock;
        public ProgressBar ProgressBar;
    }
}
