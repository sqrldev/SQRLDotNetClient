using Serilog;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.Platform.Win.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.Platform.Win
{
    /// <summary>
    /// Provides access to Windows system events that are relevant to the 
    /// clearing of QuickPass-related data from RAM, like entering
    /// an idle state, logging off or entering a sleep or hilbernation
    /// state.
    /// </summary>
    public class SystemEventNotifier : ISystemEventNotifier
    {
        private SystemEventHelperWindow _helperWindow = null;
        private CancellationTokenSource _cts = null;
        private CancellationToken _ct;
        private Task _pollTask = null;
        private bool _screensaverDetected = false;
        private bool _idleDetected = false;
        private int _maxIdleSeconds = 60*15;

        public event EventHandler<SystemEventArgs> Idle;
        public event EventHandler<SystemEventArgs> Screensaver;
        public event EventHandler<SystemEventArgs> Standby;
        public event EventHandler<SystemEventArgs> SessionLogoff;
        public event EventHandler<SystemEventArgs> SessionLock;
        public event EventHandler<SystemEventArgs> ShutdownOrRestart;

        /// <summary>
        /// Creates a new <c>SystemEventNotifier</c> instance and sets up some 
        /// required resources.
        /// </summary>
        /// <param name="maxIdleSeconds">The maximum system idle time in seconds before the
        /// <c>Idle</c> event is being raised.</param>
        public SystemEventNotifier(int maxIdleSeconds)
        {
            _maxIdleSeconds = maxIdleSeconds;
            _helperWindow = new SystemEventHelperWindow(this);
            _cts = new CancellationTokenSource();
            _ct = _cts.Token;

            _pollTask = new Task(() =>
            {
                Log.Information("SystemEventNotifier polling task started");

                while (!_ct.IsCancellationRequested)
                {
                    // Check for screensaver activation
                    if (IsScreensaverRunning())
                    {
                        if (!_screensaverDetected)
                        {
                            Screensaver?.Invoke(this, new SystemEventArgs("Screensaver"));
                            Log.Information("Detected screensaver start");
                            _screensaverDetected = true;
                        }
                    }
                    else
                    {
                        if (_screensaverDetected)
                            _screensaverDetected = false;
                    }

                    // Check system idle time
                    uint idleTime = GetIdleTimeSeconds();
                    Log.Debug("Idle time: {IdleTime}", idleTime);
                    if (idleTime > _maxIdleSeconds)
                    {
                        if (!_idleDetected)
                        {
                            Idle?.Invoke(this, new SystemEventArgs("Idle Timeout"));
                            Log.Information("Detected idle timeout");
                            _idleDetected = true;
                        }
                    }
                    else
                    {
                        if (_idleDetected)
                            _idleDetected = false;
                    }

                    Thread.Sleep(1000);
                }

                Log.Information("SystemEventNotifier polling task ending");

            }, _ct);

            _pollTask.Start();
        }

        ~SystemEventNotifier()
        {
            // Cancel the polling task
            _cts.Cancel();
        }

        private uint GetIdleTimeSeconds()
        {
            uint idleTime = 0;
            UnmanagedMethods.LASTINPUTINFO lastInputInfo = new UnmanagedMethods.LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            uint envTicks = (uint)Environment.TickCount;

            if (UnmanagedMethods.GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;
                idleTime = envTicks - lastInputTick;
            }

            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }

        private bool IsScreensaverRunning()
        {
            const int SPI_GETSCREENSAVERRUNNING = 114;

            bool isRunning = false;
            if (!UnmanagedMethods.SystemParametersInfo(SPI_GETSCREENSAVERRUNNING, 0, ref isRunning, 0))
            {
                // Could not detect screen saver status...
                return false;
            }

            return isRunning;
        }

        public IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            Log.Debug("SystemEventNotifier WndProc: MSG={Msg}, wParam={wParam}, lParam={lParam}",
                ((UnmanagedMethods.WindowsMessage)msg).ToString(),
                ((UnmanagedMethods.WindowsMessage)wParam.ToInt32()).ToString(),
                ((UnmanagedMethods.WindowsMessage)lParam.ToInt32()).ToString());

            switch (msg)
            {
                case (uint)UnmanagedMethods.WindowsMessage.WM_WTSSESSION_CHANGE:
                    switch (wParam.ToInt32())
                    {
                        case UnmanagedMethods.WTS_SESSION_LOGOFF:
                            SessionLogoff?.Invoke(this, new SystemEventArgs("Session Logoff"));
                            Log.Information("Detected session logoff");
                            break;
                        case UnmanagedMethods.WTS_SESSION_LOCK:
                            SessionLock?.Invoke(this, new SystemEventArgs("Session Lock"));
                            Log.Information("Detected session lock");
                            break;
                    }
                    break;

                case (uint)UnmanagedMethods.WindowsMessage.WM_ENDSESSION:

                    // If the session is being ended, the wParam parameter is TRUE; 
                    // the session can end any time after all applications have returned 
                    // from processing this message. Otherwise, it is FALSE.
                    if (wParam.ToInt32() == 0) break;
                    
                    switch (lParam.ToInt32())
                    {
                        case 0: //Shutdown or restart
                            ShutdownOrRestart?.Invoke(this, new SystemEventArgs("System Shutdown/Restart"));
                            Log.Information("Detected system shutdown/restart");
                            break;
                        case UnmanagedMethods.ENDSESSION_LOGOFF:
                        case UnmanagedMethods.ENDSESSION_CLOSEAPP:
                        case UnmanagedMethods.ENDSESSION_CRITICAL:
                            SessionLogoff?.Invoke(this, new SystemEventArgs("Session Ending"));
                            Log.Information("Detected end of session");
                            break;
                    }
                    break;

                case (uint)UnmanagedMethods.WindowsMessage.WM_POWERBROADCAST:

                    switch (wParam.ToInt32())
                    {
                        case UnmanagedMethods.PBT_APMSUSPEND:
                        case UnmanagedMethods.PBT_APMSTANDBY:
                            Standby?.Invoke(this, new SystemEventArgs("Standby"));
                            Log.Information("Detected entering sleep mode");
                            break;
                    }
                    break;
                    
                default:
                    break;
            }
            return IntPtr.Zero;
        }
    }


    /// <summary>
    /// A native Win32 helper window encapsulation for handling broadcast 
    /// window messages sent by the system.
    /// </summary>
    public class SystemEventHelperWindow : NativeWindow
    {
        private SystemEventNotifier _systemEventNotifier;

        /// <summary>
        /// Creates a new native (Win32) helper window for receiving broadcast window messages.
        /// </summary>
        /// <param name="systemEventNotifier">The <c>SystemEventNotifier</c> instance which will be
        /// receiving the relevant system window messages.</param>
        public SystemEventHelperWindow(SystemEventNotifier systemEventNotifier) : base()
        {
            _systemEventNotifier = systemEventNotifier;

            // Register to receiving session notification window messages
            bool success = UnmanagedMethods.WTSRegisterSessionNotification(
                this.Handle, UnmanagedMethods.NOTIFY_FOR_THIS_SESSION);
            Log.Information("Registering to receive session notifications");
        }

        ~SystemEventHelperWindow()
        {
            // Unregister from receiving session notification window messages
            UnmanagedMethods.WTSUnRegisterSessionNotification(this.Handle);
            Log.Information("Unregistering from session notifications");
        }

        /// <summary>
        /// This function will receive all the system window messages relevant to our window.
        /// </summary>
        protected override IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            Log.Debug("WndProc called on SystemEventNotifier helper window: MSG = {Msg}",
                ((UnmanagedMethods.WindowsMessage)msg).ToString());

            switch (msg)
            {
                case (uint)UnmanagedMethods.WindowsMessage.WM_WTSSESSION_CHANGE:
                case (uint)UnmanagedMethods.WindowsMessage.WM_ENDSESSION:
                case (uint)UnmanagedMethods.WindowsMessage.WM_POWERBROADCAST:
                    // Forward relevant messages to the SystemEventNotifier's window procedure
                    _systemEventNotifier.WndProc(hWnd, msg, wParam, lParam);
                    break;
            }
            return base.WndProc(hWnd, msg, wParam, lParam);
        }
    }
}
