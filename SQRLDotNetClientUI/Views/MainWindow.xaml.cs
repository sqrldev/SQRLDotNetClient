using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SQRLDotNetClientUI.AvaloniaExtensions;
using Serilog;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Platform.Win;
using SQRLDotNetClientUI.Platform;
using System;
using System.Reflection;

namespace SQRLDotNetClientUI.Views
{
    public class MainWindow : Window
    {
        public INotifyIcon NotifyIcon { get; }
        public LocalizationExtension LocalizationService {get;}
        public MainWindow()
        {
            InitializeComponent();
            if (AvaloniaLocator.Current.GetService<MainWindow>() == null)
            {
                AvaloniaLocator.CurrentMutable.Bind<MainWindow>().ToConstant(this);
            }
            this.LocalizationService = new LocalizationExtension();
#if DEBUG
            this.AttachDevTools();
#endif

            NotifyIcon = (NotifyIcon)Activator.CreateInstance(Implementation.ForType<INotifyIcon>());
            if (NotifyIcon != null)
            {
                //NotifyIcon.IconPath = @"C:\Users\Alex\Desktop\test.ico";
                string[] resNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                NotifyIcon.IconPath = @"resm:SQRLDotNetClientUI.Assets.sqrl_icon_normal_256.ico";
                NotifyIcon.DoubleClick += (s, e) =>
                {
                    Log.Information("Showing main window");
                    this.Show();
                    this.Activate();
                    this.Focus();
                };
                NotifyIcon.Show();
            }

            // Prevent that closing the main form shuts down
            // the application and only hide the main window instead.
            this.Closing += (s, e) =>
            {

                if (NotifyIcon != null)
                {
                    Log.Information("Hiding main window");
                    ((Window)s).Hide();
                    NotifyIcon.Show();
                }
                else
                {
                    Log.Information("Minimizing main window");
                    ((Window)s).WindowState = WindowState.Minimized;
                }
                e.Cancel = true;
            };

            this.Closed += (s, e) =>
            {
                // Remove the notify icon
                if (NotifyIcon != null) NotifyIcon?.Remove();
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
