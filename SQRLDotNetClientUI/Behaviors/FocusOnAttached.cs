using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace SQRLDotNetClientUI.Behaviors
{
    /// <summary>
    /// This behaviour can be set in XAML to give initial focus
    /// to a UI control.
    /// </summary>
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
