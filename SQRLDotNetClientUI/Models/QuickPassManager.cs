using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace SQRLDotNetClientUI.Models
{
    /// <summary>
    /// Provides functionality for SQRL's "QuickPass" feature.
    /// </summary>
    public sealed class QuickPassManager
    {
        private static readonly Lazy<QuickPassManager> _instance = new Lazy<QuickPassManager>(() => new QuickPassManager());
        private IdentityManager _identityManager = IdentityManager.Instance;
        private Timer _timer = new Timer();
        private byte[] _quickPass;

        /// <summary>
        /// Returns the singleton <c>QuickPassManager</c> instance. If 
        /// the instance does not exists yet, it will first be created.
        /// </summary>
        public static QuickPassManager Instance
        {
            get => _instance.Value;
        }

        /// <summary>
        /// The constructor is private because <c>QuickPassManager</c> 
        /// implements the singleton pattern. To get an instance, use 
        /// <c>QuickPassManager.Instance</c> instead.
        /// </summary>
        private QuickPassManager()
        {
            _timer.Enabled = false;
            _timer.AutoReset = false; // Dont restart timer after calling elapsed
            _timer.Elapsed += _timer_Elapsed;

            // Clear QuickPass if the current identity changed
            _identityManager.IdentityChanged += (sender, e) => ClearQuickPass();
        }

        /// <summary>
        /// Returns <c>true</c> if a QuickPass for the currently active identity
        /// is currently stored in memory, or <c>false</c> otherwise.
        /// </summary>
        public bool HasQuickPass()
        {
            // TODO: Implement
            return true;
        }

        /// <summary>
        /// Clears the QuickPass from memory. After calling this method,
        /// SQRL will ask for the full master password again for authentication.
        /// </summary>
        public void ClearQuickPass()
        {
            // First, stop the QuickPass timer if it is running
            _timer.Stop();

            // TODO: Implement

            // Finally, fire the QuickPassCleared event
            QuickPassCleared?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// This event handler gets called when the QuickPass timer has elapsed.
        /// </summary>
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ClearQuickPass();
        }

        /// <summary>
        /// This event gets fired when the QuickPass was cleared from memory.
        /// </summary>
        public EventHandler<EventArgs> QuickPassCleared;
    }

    /// <summary>
    /// This class is a container for our QuickPass management information.
    /// </summary>
    public class QuickPassInfo
    {
        public byte[] QuickPass;
        public DateTime EstablishedDate;
        public int TimeoutInSeconds;
        public int ScryptIterationCount;
    }
}
