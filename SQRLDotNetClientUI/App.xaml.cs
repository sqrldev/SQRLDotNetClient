using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MonoMac.AppKit;
using SQRLDotNetClientUI.ViewModels;
using SQRLDotNetClientUI.Views;
using System.Runtime.InteropServices;
using Serilog;
using SQRLDotNetClientUI.Models;
using System;
using SQRLDotNetClientUI.Platform;
using Avalonia.Controls;
using System.Collections.Generic;
using SQRLCommonUI.AvaloniaExtensions;
using ReactiveUI;
using SQRLUtilsLib;

namespace SQRLDotNetClientUI
{
    public class App : Application
    {
        // We establish and keep a QuickPassManager instance here
        // so that it will immediately receive system event notifications
        private QuickPassManager _quickPassManager = QuickPassManager.Instance;

        private ContextMenu _NotifyIconContextMenu = null;
        private MainWindow _mainWindow = null;

        /// <summary>
        /// Defines the minimum time between two (automated) update checks.
        /// Manual update checks are not impacted by this value.
        /// </summary>
        public static TimeSpan MinTimeBetweenUpdateChecks { get; } = new TimeSpan(0, 60, 0); // 60 minutes

        /// <summary>
        /// Gets or sets the last time the app automatically checked for updates.
        /// </summary>
        public static DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Gets or sets the app's notify icon (tray icon).
        /// </summary>
        public INotifyIcon NotifyIcon { get; set;  } = null;

        /// <summary>
        /// Provides localization services.
        /// </summary>
        public LocalizationExtension Localization { get; }

        /// <summary>
        /// Creates a new <c>App</c> instance.
        /// </summary>
        public App() : base()
        {
            this.Localization = new LocalizationExtension();
        }

        /// <summary>
        /// Initializes the app by loading the XAML.
        /// </summary>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            this._mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            
            Log.Information("App initialization completed!");
        }

        /// <summary>
        /// This override gets called when the framework initialization is completed.
        /// </summary>
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
              
                // If this is running on a Mac we need a special event handler for URL schema invokation
                // This also handles System Events and notifications, it gives us a native foothold on a Mac.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Log.Information("Initialializing App Delegate for macOS");
                    NSApplication.Init();
                    NSApplication.SharedApplication.Delegate = new Platform.OSX.AppDelegate();
                }

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
                    Log.Information("NotifyIcon implementation available, setting up NotifyIcon");

                    NotifyIcon.ToolTipText = "SQRL .NET Client";
                    NotifyIcon.IconPath = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
                                          "resm:SQRLDotNetClientUI.Assets.SQRL_icon_normal_16.png" :
                                          RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                                          @"resm:SQRLDotNetClientUI.Assets.sqrl_icon_normal_256.ico" :
                                          @"resm:SQRLDotNetClientUI.Assets.sqrl_icon_normal_256_32_icon.ico";


                    NotifyIcon.DoubleClick += (s, e) =>
                    {
                        RestoreMainWindow();
                    };

                    _NotifyIconContextMenu = new ContextMenu();
                    List<object> menuItems = new List<object>();
                    menuItems.AddRange(new[] {
                    new MenuItem() {
                        Header = Localization.GetLocalizationValue("NotifyIconContextMenuItemHeaderRestore"),
                        Command = ReactiveCommand.Create(RestoreMainWindow) },
                    new MenuItem() {
                        Header = Localization.GetLocalizationValue("NotifyIconContextMenuItemHeaderExit"),
                        Command = ReactiveCommand.Create(Exit) }
                    });
                    _NotifyIconContextMenu.Items = menuItems;
                    NotifyIcon.ContextMenu = _NotifyIconContextMenu;
                    NotifyIcon.Visible = true;
                }

                // Set up the app's main window, if we aren't staring minimized to tray
                if (!AppSettings.Instance.StartMinimized || NotifyIcon == null)
                {
                    desktop.MainWindow = _mainWindow;
                }
                
                
            }          

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Restores the app's main window by setting its <c>WindowState</c> to
        /// <c>WindowState.Normal</c> and showing the window.
        /// </summary>
        public void RestoreMainWindow()
        {
            Log.Information("Restoring main window");

            var mainWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                .MainWindow;

            if (mainWindow == null)
            {
                mainWindow = _mainWindow;
            }

            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.BringIntoView();
            mainWindow.ActivateWorkaround(); // Extension method hack because of https://github.com/AvaloniaUI/Avalonia/issues/2975
            mainWindow.Focus();

            // Again, ugly hack because of https://github.com/AvaloniaUI/Avalonia/issues/2994
            mainWindow.Width += 0.1;
            mainWindow.Width -= 0.1;
        }

        /// <summary>
        /// Exits the app by calling <c>Shutdown()</c> on the <c>IClassicDesktopStyleApplicationLifetime</c>.
        /// </summary>
        public void Exit()
        {
            (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                .Shutdown(0);
        }
    }
}
