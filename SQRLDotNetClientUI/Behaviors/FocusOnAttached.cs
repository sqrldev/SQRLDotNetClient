using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.Behaviors
{
    public class FocusOnAttached: Behavior<Control>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            Dispatcher.UIThread.Post(() =>
            {
                AssociatedObject.Focus();
            }, DispatcherPriority.Layout);
        }
    }

}
