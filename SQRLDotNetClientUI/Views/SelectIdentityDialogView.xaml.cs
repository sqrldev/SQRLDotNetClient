using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SQRLDotNetClientUI.Models;
using System;

namespace SQRLDotNetClientUI.Views
{
    public class SelectIdentityDialogView : Window
    {
        private StackPanel _stackPnlMain = null;
        private IdentityManager _identityManager = IdentityManager.Instance;

        public SelectIdentityDialogView()
        {
            this.InitializeComponent();

            this._stackPnlMain = this.FindControl<StackPanel>("stackPnlMain");
            PopulateUI();

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Retrieves a list of identities and pupulates the user interface.
        /// </summary>
        private void PopulateUI()
        {
            var currentId = _identityManager.CurrentIdentity;
            var ids = _identityManager.GetIdentities();

            Controls content = _stackPnlMain.Children;
            content.Clear();

            foreach (var id in ids)
            {
                string name = id.Item1;
                string uniqueId = id.Item2;

                RadioButton rb = new RadioButton()
                {
                    Content = name,
                    Margin = Thickness.Parse("20,10,20,10"),
                    Tag = id
                };

                if (currentId.Block0.UniqueIdentifier.ToHex() == uniqueId)
                    rb.IsChecked = true;

                rb.Click += OnIdentitySelected;

                content.Add(rb);
            }
        }

        /// <summary>
        /// Handles the event of an identity getting selected.
        /// </summary>
        private void OnIdentitySelected(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            (string name, string uniqueId) = (Tuple<string, string>)rb.Tag;

            _identityManager.SetCurrentIdentity(uniqueId);

            this.Close();
        }
    }
}
