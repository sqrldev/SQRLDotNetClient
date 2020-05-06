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
using System.Threading;
using System.Net;
using Avalonia.Threading;
using System.Text;
using System.Web;
using System.Linq;

namespace SQRLDotNetClientUI
{
    public class App : Application
    {
        private QuickPassManager _quickPassManager = null;
        private ContextMenu _NotifyIconContextMenu = null;
        private MainWindow _mainWindow = null;
        private readonly object _priorNutHashLockObject = new object();
        private byte[] _priorNutHash = new byte[32];
        private DateTime _lastNutHashUpdatedOn = DateTime.Now;

        /// <summary>
        /// The magic wakeup string is sent to an existing app instance
        /// to signal that the existing instances main window should be shown.
        /// This is only used if the new instance was started without any 
        /// command line arguments.
        /// </summary>
        public static readonly string MagicWakeupString = "wakeup!";

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
            SQRL.StartCPSServer();
            SQRL.CPS.CPSRequestReceived += CPSRequestReceived;
        }

        /// <summary>
        /// Handles data coming in via Inter-Process-Communication.
        /// </summary>
        /// <param name="ipcDataString">The data received by the IPC server. This sould
        /// either be a valid "sqrl://" URL to initiate an authentication operation, or 
        /// the magic wakeup keyword if we only want to show the main window.</param>
        public void HandleIPC(string ipcDataString)
        {
            if (ipcDataString == MagicWakeupString)
            {
                Log.Information("Showing main screen via IPC wakeup");
                ShowMainScreen();
            }
            else
            {
                Log.Information("Try handling auth request via IPC");
                HandleAuthRequest(ipcDataString, "IPC");
            }
        }

        /// <summary>
        /// This event handler gets called if the CPS server receives a new CPS request.
        /// </summary>
        private void CPSRequestReceived(object sender, CPSRequestReceivedEventArgs e)
        {
            // We're not interested in "gif" queries
            if (e.Context.Request.RawUrl.EndsWith(".gif")) return;

            // Extract and base64-decode the sqrl:// URL from the cps request
            string data = e.Context.Request.Url.AbsolutePath.Substring(1);
            string sqrlUrl = Encoding.UTF8.GetString(Sodium.Utilities.Base64ToBinary(
                data, "", Sodium.Utilities.Base64Variant.UrlSafeNoPadding));

            Log.Information("Try handling auth request via CPS");
            HandleAuthRequest(sqrlUrl, "CPS");
        }

        /// <summary>
        /// Checks if an auth request was already performed for the nut embedded within
        /// <paramref name="sqrlUrl"/>, and if not, fires up the authentication screen.
        /// Any auth request for a nut which was already handled will be ignored.
        /// This handles the "race condition" that arises in our new approach where both
        /// CPS and a sqrl:// scheme invocation can trigger an authentication popup.
        /// </summary>
        /// <param name="sqrlUrl">The "sqrl://" authentication URL.</param>
        /// <param name="source">A string representing the caller of the method. Used for logging.</param>
        private void HandleAuthRequest(string sqrlUrl, string source)
        {
            try
            {
                Uri uri = new Uri(sqrlUrl);
                string nut = HttpUtility.ParseQueryString(uri.Query).Get("nut");
                if (string.IsNullOrEmpty(nut)) return;

                byte[] hashedNut = Sodium.CryptoHash.Sha256(nut);
                bool isFirstAuthForThisNut = false;

                Log.Information($"Locking last nut hash for {source} from Thread id {Thread.CurrentThread.ManagedThreadId}");
                lock (this._priorNutHashLockObject)
                {
                    // First, let's check our timing:
                    // If the last auth request using a fresh nut happend longer than 
                    // 3 seconds ago, we can assume that the current request does not belong
                    // to the same authentication event as that prior request, so we clear 
                    // the "last nut" hash and start over fresh.
                    // (We have to take into account that either of the two methods, CPS or
                    // sqrl:// scheme invocation, could not be available, so clearing on the
                    // second event with the same nut may not always happen!)
                    TimeSpan timeSinceLastUpdate = _lastNutHashUpdatedOn - DateTime.Now;
                    if (timeSinceLastUpdate.TotalSeconds > 3)
                    {
                        this._priorNutHash.ZeroFill();
                    }

                    // Now, let's check if the current request's hashed nut
                    // matches the one from the last request
                    if (!this._priorNutHash.SequenceEqual(hashedNut))
                    {
                        // Hashes do not match, so this is the first 
                        // auth request for this nut. 
                        this._priorNutHash = hashedNut;
                        isFirstAuthForThisNut = true;
                        _lastNutHashUpdatedOn = DateTime.Now;
                    }
                    else
                    {
                        // We've seen this hash before, so we're coming
                        // in second. Let's clear our hash.
                        this._priorNutHash.ZeroFill();
                    }
                }

                if (isFirstAuthForThisNut)
                {
                    Log.Information($"Handling auth request via {source} succeeded, seems we came in first!");
                    ShowAuthScreen(uri);
                }
                else
                {
                    Log.Information($"Handling auth request via {source} aborted, already handled by other source!");
                }
            }
            catch (UriFormatException ufe)
            {
                Log.Error("Got CPS request with invalid URI!\r\n{UriFormatException}", ufe);
            }
            catch (Exception ex)
            {
                Log.Error("Error in CPSRequestReceived event handler:\r\n{Exception}", ex);
            }
        }

        /// <summary>
        /// Displays the app's authentication screen.
        /// </summary>
        /// <param name="uri"></param>
        private void ShowAuthScreen(Uri uri)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var mainMenuViewModel = ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;

                AuthenticationViewModel authView = new AuthenticationViewModel(uri);
                mainMenuViewModel.AuthVM = authView;
                ((MainWindowViewModel)_mainWindow.DataContext).Content = authView;

                Log.Information("Showing auth screen!");

                RestoreMainWindow();
            });
        }

        /// <summary>
        /// Displays the app's main screen.
        /// </summary>
        private void ShowMainScreen()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var mainMenuViewModel = ((MainWindowViewModel)_mainWindow.DataContext).MainMenu;
                ((MainWindowViewModel)_mainWindow.DataContext).Content = mainMenuViewModel;

                RestoreMainWindow();
            });
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

                // We establish and keep a QuickPassManager instance here
                // so that it will immediately receive system event notifications
                _quickPassManager = QuickPassManager.Instance;

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
