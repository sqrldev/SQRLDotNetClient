using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Serilog;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;

namespace SQRLDotNetClientUI.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (AvaloniaLocator.Current.GetService<MainWindow>() == null)
            {
                AvaloniaLocator.CurrentMutable.Bind<MainWindow>().ToConstant(this);
            }
#if DEBUG
            this.AttachDevTools();
#endif

            // Prevent the main window from closing. Just hide it instead
            // if we have a notify icon, or minimize it otherwise.
            this.Closing += (s, e) =>
            {
                if ((App.Current as App).NotifyIcon != null)
                {
                    Log.Information("Hiding main window instead of closing it");
                    Dispatcher.UIThread.Post(() =>
                    {
                        ((Window)s).Hide();
                    });
                    (App.Current as App).NotifyIcon.Visible = true;
                }
                else
                {
                    Log.Information("Minimizing main window instead of closing it");
                    Dispatcher.UIThread.Post(() =>
                    {
                        ((Window)s).WindowState = WindowState.Minimized;
                    });
                }
                e.Cancel = true;
            };
        }

        // This would be ideal for the notification icon, but unfortunately
        // it causes the main window to only show a black screen when showing
        // the window again.Probably an Avalonia bug.

        protected override void HandleWindowStateChanged(WindowState state)
        {
            if (state == WindowState.Minimized && (App.Current as App).NotifyIcon != null)
            {
                Log.Information("Hiding main window instead of minimizing it");
                this.Hide();
            }
            else
            {
                base.HandleWindowStateChanged(state);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Exits the app
        /// </summary>
        public void Exit()
        {
            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                .Shutdown(0);
        }
    }
}
