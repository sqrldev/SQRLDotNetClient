using Avalonia;
using MonoMac.AppKit;
using MonoMac.Foundation;
using Serilog;
using SQRLDotNetClientUI.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.Platform.OSX
{
    public class SystemEventNotifier : ISystemEventNotifier
    {

        /// <summary>
        /// Specifies the checking interval for the polling thread.
        /// </summary>
        private readonly int POLL_INTERVAL = 5000;


        private CancellationTokenSource _cts = null;
        private CancellationToken _ct;
        private Task _pollTask = null;
        private bool _screensaverDetected = false;
        private bool _idleDetected = false;
        private int _maxIdleSeconds = 60 * 15; // Set a sensible default, will be overwritten anyway

        public event EventHandler<SystemEventArgs> Idle;
        public event EventHandler<SystemEventArgs> Screensaver;
        public event EventHandler<SystemEventArgs> Standby;
        public event EventHandler<SystemEventArgs> SessionLogoff;
        public event EventHandler<SystemEventArgs> SessionLock;
        public event EventHandler<SystemEventArgs> ShutdownOrRestart;

        public SystemEventNotifier(int maxIdleSeconds = 60 * 15)
        {
            
            var appleAppDelegate = AvaloniaLocator.Current.GetService<AppDelegate>();
          
            this._maxIdleSeconds = maxIdleSeconds;
            ((NSDistributedNotificationCenter)NSDistributedNotificationCenter.DefaultCenter).AddObserver(new NSString("com.apple.screenIsLocked"), (obj) =>
            {
                Log.Information("Detected session lock");
                Screensaver?.Invoke(this, new SystemEventArgs("Session Lock"));
            });

            ((NSDistributedNotificationCenter)NSDistributedNotificationCenter.DefaultCenter).AddObserver(new NSString("com.apple.screensaver.didstart"), (obj) =>
            {
                Log.Information("Detected Screen Saver");
                Screensaver?.Invoke(this, new SystemEventArgs("Screensaver"));
                

            });

            NSWorkspace.Notifications.ObserveWillSleep((s, e) =>
            {
                Log.Information("Detected Standy");
                Standby?.Invoke(this, new SystemEventArgs("Stand By"));
            });

            NSWorkspace.Notifications.ObserveWillPowerOff((s, e) =>
            {
                Log.Information("Detected PowerOff / Reboot");
                ShutdownOrRestart?.Invoke(this, new SystemEventArgs("System ShutDown / Reboot"));
            });

            NSWorkspace.Notifications.ObserveSessionDidResignActive((s, e) =>
            {
                Log.Information("Detected PowerOff / Reboot");
                SessionLogoff?.Invoke(this, new SystemEventArgs("Session Log Off"));

            });
            


            _pollTask = new Task(() =>
            {
                Log.Information("SystemEventNotifier polling task started");
                Thread.Sleep(POLL_INTERVAL); //Sleep at first to give Mac Delegate time
                while (!_ct.IsCancellationRequested)
                {
                    // Check system idle time
                    TimeSpan idletime = AppDelegate.CheckIdleTime();
                    Log.Debug("Idle time: {IdleTime}", idletime.ToString());
                    if (idletime.TotalSeconds > _maxIdleSeconds)
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

                    Thread.Sleep(POLL_INTERVAL);
                }

                Log.Information("SystemEventNotifier polling task ending");

            }, _ct);

            _pollTask.Start();
        }

    }

}
