using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SQRLDotNetClientUI.AvaloniaExtensions;
using Serilog;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Platform.Win;
using SQRLDotNetClientUI.Platform;
using System;
using System.Collections.Generic;
using ReactiveUI;
using System.Runtime.InteropServices;

namespace SQRLDotNetClientUI.Views
{
    public class MainWindow : Window
    {
        private bool _reallyClose = false;
        private ContextMenu _NotifyIconContextMenu;

        // We establish and keep a QuickPassManager instance here
        // so that it will immediately receive system event notifications
        private QuickPassManager _quickPassManager = QuickPassManager.Instance;

        public INotifyIcon NotifyIcon { get; } = null;
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

            // Set up and configure the notification icon
            // Get the type of the platform-specific implementation
            Type type = Implementation.ForType<INotifyIcon>();
            if (type != null)
            {
                // If we have one, create an instance for it
                 NotifyIcon = (INotifyIcon)Activator.CreateInstance(type);
            }

            if (NotifyIcon != null)
            {
                NotifyIcon.ToolTipText = "SQRL .NET Client";
                NotifyIcon.IconPath = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)? "resm:SQRLDotNetClientUI.Assets.SQRL_icon_normal_16.png" : @"resm:SQRLDotNetClientUI.Assets.sqrl_icon_normal_256.ico";
                NotifyIcon.DoubleClick += (s, e) =>
                {
                    RestoreWindow();
                };

                _NotifyIconContextMenu = new ContextMenu();
                List<object> menuItems = new List<object>();
                menuItems.AddRange(new[] {
                    new MenuItem() {
                        Header = LocalizationService.GetLocalizationValue("NotifyIconContextMenuItemHeaderRestore"),
                        Command = ReactiveCommand.Create(RestoreWindow) },
                    new MenuItem() { 
                        Header = LocalizationService.GetLocalizationValue("NotifyIconContextMenuItemHeaderExit"), 
                        Command = ReactiveCommand.Create(Exit) }
                    });
                _NotifyIconContextMenu.Items = menuItems;
                NotifyIcon.ContextMenu = _NotifyIconContextMenu;
                NotifyIcon.Visible = true;
            }

            // Prevent the main window from closing. Just hide it instead
            // if we have a notify icon, or minimize it otherwise.
            this.Closing += (s, e) =>
            {
                if (_reallyClose) return;

                if (NotifyIcon != null)
                {
                    Log.Information("Hiding main window");
                    ((Window)s).Hide();
                    NotifyIcon.Visible = true;
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
                // Remove the notify icon when the main window closes
                if (NotifyIcon != null) NotifyIcon?.Remove();
            };
        }

        private void RestoreWindow()
        {
            Log.Information("Restoring main window from notification icon");
            this.WindowState = WindowState.Normal;
            this.Show();
            this.BringIntoView();
            this.Activate();
            this.Focus();
        }

        // This would be ideal for the notification icon, but unfortunately
        // it causes the main window to only show a black screen when showing
        // the window again. Probably an Avalonia bug.

        //protected override void HandleWindowStateChanged(WindowState state)
        //{           
        //    if (state == WindowState.Minimized && NotifyIcon != null)
        //    {
        //        this.Hide();
        //    }
        //    else base.HandleWindowStateChanged(state);

        //}

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Exits the app by closing the main window.
        /// </summary>
        private void Exit()
        {
            _reallyClose = true;
            this.Close();
        }
    }
}
