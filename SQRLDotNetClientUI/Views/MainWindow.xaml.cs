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

namespace SQRLDotNetClientUI.Views
{
    public class MainWindow : Window
    {
        private bool _reallyClose = false;
        private ContextMenu _NotifyIconContextMenu;

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

            NotifyIcon = (NotifyIcon)Activator.CreateInstance(
                Implementation.ForType<INotifyIcon>());

            if (NotifyIcon != null)
            {
                NotifyIcon.ToolTipText = "SQRL .NET Client";
                NotifyIcon.IconPath = @"resm:SQRLDotNetClientUI.Assets.sqrl_icon_normal_256.ico";
                NotifyIcon.DoubleClick += (s, e) =>
                {
                    Log.Information("Restoring main window from notification icon");
                    this.WindowState = WindowState.Normal;
                    this.Show();
                    this.BringIntoView();
                    this.Activate();
                    this.Focus();
                };

                _NotifyIconContextMenu = new ContextMenu();
                List<object> menuItems = new List<object>();
                menuItems.AddRange(new[] {
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
