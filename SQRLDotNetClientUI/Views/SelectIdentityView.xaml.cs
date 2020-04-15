using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.ViewModels;
using System;

namespace SQRLDotNetClientUI.Views
{
    public class SelectIdentityView : UserControl
    {
        private StackPanel _stackPnlMain = null;
        private IdentityManager _identityManager = IdentityManager.Instance;

        public SelectIdentityView()
        {
            this.InitializeComponent();
            PopulateUI();
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
            _stackPnlMain = this.FindControl<StackPanel>("stackPnlMain");
            var currentId = _identityManager.CurrentIdentity;
            var currentIdUniqueId = _identityManager.CurrentIdentityUniqueId;
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

                if (currentIdUniqueId == uniqueId)
                    rb.IsChecked = true;

                rb.Click += OnIdentitySelected;

                content.Add(rb);
            }
        }

        /// <summary>
        /// Event handler that gets called when an identity gets selected.
        /// </summary>
        private void OnIdentitySelected(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            (string name, string uniqueId) = (Tuple<string, string>)rb.Tag;

            SelectIdentityViewModel viewModel = (SelectIdentityViewModel)this.DataContext;
            viewModel.OnIdentitySelected(uniqueId);
        }
    }
}
