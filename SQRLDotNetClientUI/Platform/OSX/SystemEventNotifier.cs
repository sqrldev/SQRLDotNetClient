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
    /// <summary>
    /// Provides access to MacOSX system events that are relevant to the 
    /// clearing of QuickPass-related data from RAM, like entering
    /// an idle state, logging off or entering a sleep or hilbernation
    /// state etc.
    /// 
    /// Most of the events are gathered by creating a native Apple App Delegate (at App Initialize) and 
    /// registering and listening to event related window messages. Some
    /// events require polling, so a low-impact polling thread is implemented
    /// to peridodically check for those events.
    /// </summary>
    public class SystemEventNotifier : ISystemEventNotifier
    {

        /// <summary>
        /// Specifies the checking interval for the polling thread.
        /// </summary>
        private readonly int POLL_INTERVAL = 5000;


        private CancellationTokenSource _cts = null;
        private CancellationToken _ct;
        private Task _pollTask = null;
        
        private bool _idleDetected = false;
        private int _maxIdleSeconds = 60 * 15; // Set a sensible default, will be overwritten anyway

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
        public SystemEventNotifier(int maxIdleSeconds = 60 * 15)
        {
            Log.Information($"Instanciating System Event Notifier");
            _cts = new CancellationTokenSource();
            _ct = _cts.Token;

            this._maxIdleSeconds = maxIdleSeconds;
            
            //Subscribe to an apple notification fired when the screen is locked
            ((NSDistributedNotificationCenter)NSDistributedNotificationCenter.DefaultCenter).AddObserver(new NSString("com.apple.screenIsLocked"), (obj) =>
            {
                Log.Information("Detected session lock");
                SessionLock?.Invoke(this, new SystemEventArgs("Session Lock"));
            });

            //Subscribe to an apple notification fired when the screen saver starts
            ((NSDistributedNotificationCenter)NSDistributedNotificationCenter.DefaultCenter).AddObserver(new NSString("com.apple.screensaver.didstart"), (obj) =>
            {
                Log.Information("Detected Screen Saver");
                Screensaver?.Invoke(this, new SystemEventArgs("Screensaver"));
            });


            //Subscribe to an apple notification fired when the System goes to Sleep
            NSWorkspace.Notifications.ObserveWillSleep((s, e) =>
            {
                Log.Information("Detected Standy");
                Standby?.Invoke(this, new SystemEventArgs("Stand By"));
            });


            //Subscribe to an apple notification fired when the System goes to power off / reboot
            NSWorkspace.Notifications.ObserveWillPowerOff((s, e) =>
            {
                Log.Information("Detected PowerOff / Reboot");
                ShutdownOrRestart?.Invoke(this, new SystemEventArgs("System ShutDown / Reboot"));
            });



            //Subscribe to an apple notification fired when the System goes to Log off the current User
            NSWorkspace.Notifications.ObserveSessionDidResignActive((s, e) =>
            {
                Log.Information("Detected PowerOff / Reboot");
                SessionLogoff?.Invoke(this, new SystemEventArgs("Session Log Off"));

            });


            // Start a task to poll Idle Time Global Environment Variable
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
            Log.Information($"System Event Notifier was Innitialized Successfully");
        }
        ~SystemEventNotifier()
        {
            // Cancel the polling task
            _cts.Cancel();
        }
    }

   

}
