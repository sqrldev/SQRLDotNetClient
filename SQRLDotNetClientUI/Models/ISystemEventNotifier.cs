using System;

namespace SQRLDotNetClientUI.Models
{
    /// <summary>
    /// Provides access to system events that are relevant to the 
    /// clearing of QuickPass-related data from RAM, like entering
    /// an idle state, logging off or entering a sleep or hilbernation
    /// state.
    /// 
    /// Platform-specific implementations for this interface can be found in 
    /// <c>SQRLDotNetClientUI.Platform.XXX</c>.
    /// 
    /// </summary>
    public interface ISystemEventNotifier
    {
        /// <summary>
        /// This event is fired when the system is in an idle state
        /// (no user input) for longer than the given time.
        /// </summary>
        public event EventHandler<SystemEventArgs> Idle;

        /// <summary>
        /// This event is fired when the screensaver starts running.
        /// </summary>
        public event EventHandler<SystemEventArgs> Screensaver;

        /// <summary>
        /// This event is fired when the system enters any sleep state
        /// like standby or hilbernate.
        /// </summary>
        public event EventHandler<SystemEventArgs> Standby;

        /// <summary>
        /// This event is fired when the user logg off the current session.
        /// </summary>
        public event EventHandler<SystemEventArgs> SessionLogoff;

        /// <summary>
        /// This event is fired when the user locks the current session.
        /// </summary>
        public event EventHandler<SystemEventArgs> SessionLock;

        /// <summary>
        /// This event is fired when the system is being shut down or restarted.
        /// </summary>
        public event EventHandler<SystemEventArgs> ShutdownOrRestart;
    }
}
