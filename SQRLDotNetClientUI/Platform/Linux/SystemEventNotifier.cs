using Serilog;
using SQRLDotNetClientUI.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.Platform.Linux
{
    public class SystemEventNotifier : ISystemEventNotifier
    {
        private readonly int POLL_INTERVAL = 5000;

        private enum ScreenSaverState
        {
            ScreenSaverOff=0,
            ScreenSaverOn=1,
            ScreenSaverCycle=2,
            ScreenSaverDisabled=3
        }

        private CancellationTokenSource _cts = null;
        private CancellationToken _ct;
        private Task _pollTask = null;

        private bool _idleDetected = false;
        private bool _screenSaver = false;
        private int _maxIdleSeconds = 60 * 15; // Set a sensible default, will be overwritten anyway

        public event EventHandler<SystemEventArgs> Idle;
        public event EventHandler<SystemEventArgs> Screensaver;
        public event EventHandler<SystemEventArgs> Standby;
#pragma warning disable 67 // Get rid of event "not used" warnings

        //There just isn't any consistent way to determine when Linux is doing these  
        public event EventHandler<SystemEventArgs> SessionLogoff;
        public event EventHandler<SystemEventArgs> SessionLock;
        public event EventHandler<SystemEventArgs> ShutdownOrRestart;
#pragma warning restore 67
        private IntPtr Display;
        private X11.XScreenSaverInfo xsi;

        private DateTime lastCheck;

        public SystemEventNotifier(int maxIdleSeconds = 60 * 15)
        {
            
            _cts = new CancellationTokenSource();
            _ct = _cts.Token;

            this._maxIdleSeconds = maxIdleSeconds;
            Display = X11.Xlib.XOpenDisplay(null);
            xsi =  new X11.XScreenSaverInfo();
            lastCheck = DateTime.Now;
            _pollTask = new Task(() =>
            {
                Log.Information("SystemEventNotifier polling task started");
                
                while (!_ct.IsCancellationRequested)
                {
                    // Check system idle time
                    X11.Xlib.XScreenSaverQueryInfo(Display, X11.Xlib.XRootWindow(Display, 0), ref xsi);
                    
                    Log.Debug("Idle time: {IdleTime}", xsi.idle.ToString());
                    if ((xsi.idle/1000) > (ulong)_maxIdleSeconds)
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

                    if ((ScreenSaverState)xsi.state == ScreenSaverState.ScreenSaverOn)
                    {
                        if (!_screenSaver)
                        {
                            Screensaver?.Invoke(this, new SystemEventArgs("Screen Saver"));
                            Log.Information("Detected Screen Saver On");
                            _screenSaver = true;
                        }
                    }
                    else if (_screenSaver)
                        _screenSaver =false;


                    //Since we can't check for Lock, Logout or Restart we are going to
                    //Do a bit of  a hack and assume that if our loop took longer than triple the standard time to run
                    //then we want to go ahead and clear the quick pass;
                    if ((DateTime.Now -lastCheck).TotalSeconds > (POLL_INTERVAL/1000) * 3)
                    {
                        Log.Information("Detected an Anommaly System Loop took > 3X to run, clearing quick pass");
                        Standby?.Invoke(this, new SystemEventArgs("System Hiccup Clearing QuickPass Just in Case"));
                    }
                    lastCheck = DateTime.Now;


                    Thread.Sleep(POLL_INTERVAL);
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
    }
}
