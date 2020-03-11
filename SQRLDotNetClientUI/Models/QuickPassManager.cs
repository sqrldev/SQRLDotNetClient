using Sodium;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Serilog;
using SQRLDotNetClientUI.Platform;

namespace SQRLDotNetClientUI.Models
{
    /// <summary>
    /// Provides functionality for SQRL's "QuickPass" feature.
    /// To get the sinleton instance of the class, just use the 
    /// static property <c>QuickPassManager.Instance</c> instead
    /// of calling the constructor.
    /// </summary>
    public sealed class QuickPassManager
    {
        private static QuickPassManager _instance = null;
        private IdentityManager _identityManager = IdentityManager.Instance;
        private object _dataSyncObj = new object();
        private Dictionary<string, QuickPassItem> _quickPassItems = new Dictionary<string, QuickPassItem>();
        private ISystemEventNotifier _systemEventNotifier = null;

        /// <summary>
        /// The number of seconds the QuickPass shall be run through the PBKDF.
        /// </summary>
        private const int QP_KEYDERIV_SEC = 1;

        /// <summary>
        /// Returns the singleton <c>QuickPassManager</c> instance. If 
        /// the instance does not exists yet, it will first be created.
        /// </summary>
        public static QuickPassManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new QuickPassManager();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets or sets whether all QuickPass entries should be cleared
        /// if the currently selected identity changes.
        /// </summary>
        public bool ClearQuickPassOnIdentityChange { get; set; } = false;

        /// <summary>
        /// The constructor is private because <c>QuickPassManager</c> 
        /// implements the singleton pattern. To get an instance, use 
        /// <c>QuickPassManager.Instance</c> instead.
        /// </summary>
        private QuickPassManager()
        {
            // Register the identity changed event handler
            _identityManager.IdentityChanged += _identityManager_IdentityChanged;

            int idleTimeoutSecs = (int)_identityManager?.CurrentIdentity?.Block1?.PwdTimeoutMins * 60;

            // Set up a system event notifier
            _systemEventNotifier = (ISystemEventNotifier)Activator.CreateInstance(
                Implementation.ForType<ISystemEventNotifier>(), new object[] { idleTimeoutSecs });

            if (_systemEventNotifier != null)
            {
                // Register the handler for system events
                _systemEventNotifier.SessionLock += HandleBlankingEvents;
                _systemEventNotifier.Screensaver += HandleBlankingEvents;
                _systemEventNotifier.ShutdownOrRestart += HandleBlankingEvents;
                _systemEventNotifier.Standby += HandleBlankingEvents;
                _systemEventNotifier.SessionLogoff += HandleSessionEvents;
                _systemEventNotifier.Idle += HandleIdleEvent;
            }

            Log.Information("QuickPassManager initialized.");
        }

        ~QuickPassManager()
        {
            Log.Information("QuickPassManager desctructor called");

            // Unregister the identity changed event handler
            _identityManager.IdentityChanged -= _identityManager_IdentityChanged;

            // Unregister the handler for system events
            _systemEventNotifier.SessionLock -= HandleBlankingEvents;
            _systemEventNotifier.Screensaver -= HandleBlankingEvents;
            _systemEventNotifier.ShutdownOrRestart -= HandleBlankingEvents;
            _systemEventNotifier.Standby -= HandleBlankingEvents;
            _systemEventNotifier.SessionLogoff -= HandleSessionEvents;
            _systemEventNotifier.Idle -= HandleIdleEvent;

            //Clear all QuickPass entries
            ClearAllQuickPass(QuickPassClearReason.Unspecified);
        }

        private void HandleBlankingEvents(object sender, SystemEventArgs e)
        {
            if (_identityManager.CurrentIdentity.Block1.OptionFlags.ClearQuickPassOnSleep)
            {
                Log.Information("Clearing QuickPass. Reason: {QuickPassClearReason}",
                    e.EventDescription);

                ClearAllQuickPass(QuickPassClearReason.EnterBlankingState);
            }
        }

        /// <summary>
        /// Handles system event notifications regarding idle timeouts which are 
        /// relevant for the clearing of QuickPass-related data.
        /// </summary>
        private void HandleIdleEvent(object sender, SystemEventArgs e)
        {
            if (_identityManager.CurrentIdentity.Block1.OptionFlags.ClearQuickPassOnIdle)
            {
                Log.Information("Clearing QuickPass. Reason: {QuickPassClearReason}",
                       e.EventDescription);

                ClearAllQuickPass(QuickPassClearReason.IdleTimeout);
            }
        }

        /// <summary>
        /// Handles system event notifications regarding user sessions which are 
        /// relevant for the clearing of QuickPass-related data.
        /// </summary>
        private void HandleSessionEvents(object sender, SystemEventArgs e)
        {
            if (_identityManager.CurrentIdentity.Block1.OptionFlags.ClearQuickPassOnSwitchingUser)
            {
                Log.Information("Clearing QuickPass. Reason: {QuickPassClearReason}",
                    e.EventDescription);

                ClearAllQuickPass(QuickPassClearReason.UserSwitching);
            }
        }

        /// <summary>
        /// This event handler gets called when the currently selected identity changes.
        /// </summary>
        private void _identityManager_IdentityChanged(object sender, IdentityChangedEventArgs e)
        {
            if (ClearQuickPassOnIdentityChange)
            {
                Log.Information("Clearing all QuickPass entries because of identity change");
                ClearAllQuickPass(QuickPassClearReason.IdentityChange);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if a QuickPass for the given <paramref name="identityUniqueId"/>
        /// is currently stored in memory, or <c>false</c> otherwise. To check for the currenty
        /// active identity, just pass <c>null</c> for <paramref name="identityUniqueId"/>.
        /// </summary>
        /// <param name="identityUniqueId">The hex representation of the identity's unique id (block 0)
        /// for which to check the QuickPass status, or <c>null</c> if you want to check for the current identity.</param>
        public bool HasQuickPass(string identityUniqueId = null)
        {
            if (identityUniqueId == null)
                identityUniqueId = _identityManager.CurrentIdentity?.Block0?.UniqueIdentifier?.ToHex();

            if (identityUniqueId == null)
                return false;

            bool hasQuickPass;
            lock (_dataSyncObj)
            {
                hasQuickPass = _quickPassItems.ContainsKey(identityUniqueId);
            }

            return hasQuickPass;
        }

        /// <summary>
        /// Creates a QuickPass from the given <paramref name="password"/> and
        /// <paramref name="imk"/> using the QuickPass settings stored in 
        /// <paramref name="identity"/>, stores it in memory and establishes a 
        /// timer that will clear the QuickPass after the timeout set forth in
        /// <paramref name="identity"/>'s QuickPass settings.
        /// </summary>
        /// <param name="password">The full identity master password.</param>
        /// <param name="imk">The identity's unencrypted Identity Master Key (IMK).</param>
        /// <param name="ilk">The identity's unencrypted Identity Lock Key (ILK).</param>
        /// <param name="identity">The identity that the QuickPass should be set for.</param>
        /// <param name="progress">An object implementing the IProgress interface for tracking the operation's progress (optional).</param>
        /// <param name="progressText">A string representing a text descrition for the progress indicator (optional).</param>
        public async void SetQuickPass(string password, byte[] imk, byte[] ilk, SQRLIdentity identity, IProgress<KeyValuePair<int, string>> progress = null, string progressText = null)
        {
            QuickPassItem qpi = new QuickPassItem()
            {
                EstablishedDate = DateTime.Now,
                QuickPassLength = identity.Block1.HintLength,
                IdentityUniqueId = identity.Block0.UniqueIdentifier.ToHex(),
                ScryptRandomSalt = SodiumCore.GetRandomBytes(16),
                Nonce = SodiumCore.GetRandomBytes(24),
                QuickPassTimeoutSecs = identity.Block1.PwdTimeoutMins * 60,
                ClearQuickPassOnIdle = identity.Block1.OptionFlags.ClearQuickPassOnIdle,
                ClearQuickPassOnSleep = identity.Block1.OptionFlags.ClearQuickPassOnSleep,
                ClearQuickPassOnSwitchingUser = identity.Block1.OptionFlags.ClearQuickPassOnSwitchingUser,
                Timer = new Timer()
                };

            qpi.Timer.Enabled = false;
            qpi.Timer.AutoReset = false; // Dont restart timer after calling elapsed
            qpi.Timer.Interval = qpi.QuickPassTimeoutSecs * 1000;
            qpi.Timer.Elapsed += _timer_Elapsed;

            string quickPass = password.Substring(0, qpi.QuickPassLength);

            KeyValuePair<int, byte[]> enscryptResult = await SQRL.EnScryptTime(
                quickPass, 
                qpi.ScryptRandomSalt, 
                (int)Math.Pow(2, 9), 
                QP_KEYDERIV_SEC, 
                progress, 
                progressText);

            qpi.ScryptIterationCount = enscryptResult.Key;
            qpi.EncryptedImk = StreamEncryption.Encrypt(imk, qpi.Nonce, enscryptResult.Value);
            qpi.EncryptedIlk = StreamEncryption.Encrypt(ilk, qpi.Nonce, enscryptResult.Value);

            // If we already have a QuickPass entry for this identity, remove it first
            if (HasQuickPass(qpi.IdentityUniqueId)) 
                ClearQuickPass(qpi.IdentityUniqueId, QuickPassClearReason.Unspecified);

            // Now, add the QuickPass item to our list and start the timer
            lock (_dataSyncObj)
            {
                _quickPassItems.Add(qpi.IdentityUniqueId, qpi);
                qpi.Timer.Start();
            }

            Log.Information("QuickPass set for identity {IdentityUniqueId}", 
                qpi.IdentityUniqueId);
        }

        /// <summary>
        /// Decrypts the Identity Master Key (IMK) for for the given <paramref name="identityUniqueId"/>
        /// using the provided <paramref name="quickPass"/> and returns it. If <c>null</c> is passed for
        /// <paramref name="identityUniqueId"/>, the decrypted IML for the current identity will be returned.
        /// </summary>
        /// <param name="quickPass">The QuickPass (first x characters from the identity's master password
        /// which was used to encrypt the IMK when setting the QuickPass entry for the identity.</param>
        /// <param name="identityUniqueId">The hex representation of the identity's unique id (block 0),
        /// or <c>null</c> if you want to decrypt the current identity's IMK.</param>
        /// <param name="progress">An object implementing the IProgress interface for tracking the operation's progress (optional).</param>
        /// <param name="progressText">A string representing a text descrition for the progress indicator (optional).</param>
        /// <returns>The decrypted block 1 keys (IMK, ILK) for the given <paramref name="identityUniqueId"/>, or
        /// <c>null</c> if no such QuickPass entry exists.</returns>
        public async Task<QuickPassDecryptedKeys> GetQuickPassDecryptedImk(string quickPass, string identityUniqueId = null, 
            IProgress<KeyValuePair<int, string>> progress = null, string progressText = null)
        {
            QuickPassItem qpi = null;

            if (identityUniqueId == null)
                identityUniqueId = _identityManager.CurrentIdentity?.Block0?.UniqueIdentifier?.ToHex();

            if (identityUniqueId == null)
            {
                Log.Error("Could not resolve current identity in {MethodName}, throwing Exception!", 
                    nameof(GetQuickPassDecryptedImk));

                throw new InvalidOperationException("Cannot return QuickPass-decrypted IMK without an identity!");
            }
            
            lock (_dataSyncObj)
            {
                if (!_quickPassItems.ContainsKey(identityUniqueId))
                {
                    Log.Warning("No identity found for id {IdentityUniqueId} in {MethodName}", 
                        identityUniqueId, nameof(GetQuickPassDecryptedImk));

                    return null;
                }

                qpi = _quickPassItems[identityUniqueId];
            }

            byte[] key = await SQRL.EnScryptCT(quickPass, qpi.ScryptRandomSalt, (int)Math.Pow(2, 9), 
                qpi.ScryptIterationCount, progress, progressText);

            byte[] decryptedImk = StreamEncryption.Decrypt(qpi.EncryptedImk, qpi.Nonce, key);
            byte[] decryptedIlk = StreamEncryption.Decrypt(qpi.EncryptedIlk, qpi.Nonce, key);

            Log.Information("QuickPass retrieved for identity {IdentityUniqueId}",
                identityUniqueId);

            return new QuickPassDecryptedKeys(decryptedImk, decryptedIlk);
        }

        /// <summary>
        /// Clears the QuickPass entries of all identities from memory. After calling 
        /// this method, SQRL will ask for the full master password again for all
        /// available identities.
        /// </summary>
        /// <param name="reason">The reason for clearing the QuickPass.</param>
        /// <param name="combineEvents">If set to <c>true</c>, <c>QuickPassManager</c>
        /// will not raise individual <c>QuickPassCleared</c> events for each of the
        /// QuickPass entries affected, but instead raise a combined event.</param>
        public void ClearAllQuickPass(QuickPassClearReason reason, bool combineEvents = true)
        {
            Log.Verbose("{MethodName} called", nameof(ClearAllQuickPass));

            List<string> uniqueIdsAvailable = null;
            List<string> uniqueIdsCleared = new List<string>();

            lock (_dataSyncObj)
            {
                uniqueIdsAvailable = new List<string>(_quickPassItems.Keys);
            }

            foreach (string uniqueId in uniqueIdsAvailable)
            {
                if (ClearQuickPass(uniqueId, reason, !combineEvents))
                {
                    uniqueIdsCleared.Add(uniqueId);
                }
            }

            // Only fire a combined QuickPassCleared event if combining events is enabled 
            // and if any QuickPass entries have actually been cleared.
            if (combineEvents && uniqueIdsCleared.Count > 0)
            {
                QuickPassCleared?.Invoke(this, new QuickPassClearedEventArgs(uniqueIdsCleared));
                Log.Information("Fired combined ClickPassCleared event. Reason: {Reason}",
                    reason.ToString());
            }
        }

        /// <summary>
        /// Clears the QuickPass entries of all identities from memory. After calling 
        /// this method, SQRL will ask for the full master password again for all
        /// available identities.
        /// </summary>
        /// <param name="identityUniqueId">The unique id (block 0) of the identity for 
        /// which to clear the QuickPass entry.</param>
        /// <param name="reason">The reason for clearing the QuickPass.</param>
        /// <param name="fireClearedEvent">If set to <c>true</c>, a 
        /// <c>QuickPassCleared</c> event will be fired if the QuickPass for the given
        /// <paramref name="identityUniqueId"/> could be successfully cleared.</param>
        public bool ClearQuickPass(string identityUniqueId, QuickPassClearReason reason, bool fireClearedEvent = true)
        {
            Log.Verbose("{MethodName} called", nameof(ClearQuickPass));

            lock (_dataSyncObj)
            {
                if (!_quickPassItems.ContainsKey(identityUniqueId))
                {
                    Log.Warning("No QuickPass entry found for identity {IdentityUniqueId}", identityUniqueId);
                    return false;
                }

                QuickPassItem qpi = _quickPassItems[identityUniqueId];

                if (reason == QuickPassClearReason.IdentityChange && !ClearQuickPassOnIdentityChange) return false;
                if (reason == QuickPassClearReason.IdleTimeout && !qpi.ClearQuickPassOnIdle) return false;
                if (reason == QuickPassClearReason.EnterBlankingState && !qpi.ClearQuickPassOnSleep) return false;
                if (reason == QuickPassClearReason.UserSwitching && !qpi.ClearQuickPassOnSwitchingUser) return false;

                // First, stop the QuickPass timer
                qpi.Timer.Stop();

                // Then, overwrite the encrypted imk and ilk so that we don't 
                // leave any traces of key material in RAM.
                qpi.EncryptedImk.ZeroFill();
                qpi.EncryptedIlk.ZeroFill();

                // Delete the QuickPass entry from our dictionary
                _quickPassItems.Remove(identityUniqueId);
            }

            Log.Information("QuickPass entry cleared. ID: {IdentityUniqueId} Reason: {Reason}",
                identityUniqueId, reason.ToString());

            // Finally, fire the QuickPassCleared event
            if (fireClearedEvent)
            {
                QuickPassCleared?.Invoke(this, new QuickPassClearedEventArgs(new List<string>() { identityUniqueId }));
                Log.Information("Firing QuickPassCleared event");
            }

            return true;
        }

        /// <summary>
        /// This event handler gets called when the QuickPass timer has elapsed,
        /// indicating that the QuickPass belonging to this specific timer shall be cleared.
        /// </summary>
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            QuickPassItem qpi = null;
            Timer timer = (Timer)sender;

            // Get the QuickPassInfo that this Timer belongs to
            lock (_dataSyncObj)
            {
                foreach (var item in _quickPassItems)
                {
                    if (timer == item.Value.Timer)
                    {
                        qpi = item.Value;
                        break;
                    }
                }
            }

            Log.Information("QuckPass timer elapsed for identity id {IdentityUniqueId}",
                qpi?.IdentityUniqueId);

            // If such a QuickPassInfo item exists, clear it
            if (qpi != null)
            {
                ClearQuickPass(qpi.IdentityUniqueId, QuickPassClearReason.QuickPassTimeout);
            }
        }

        /// <summary>
        /// This event gets fired when the QuickPass was cleared from memory.
        /// </summary>
        public event EventHandler<QuickPassClearedEventArgs> QuickPassCleared;
    }

    /// <summary>
    /// A container for QuickPass management information.
    /// </summary>
    public class QuickPassItem
    {
        public byte[] EncryptedImk;
        public byte[] EncryptedIlk;
        public DateTime EstablishedDate;
        public int QuickPassLength;
        public int QuickPassTimeoutSecs;
        public int ScryptIterationCount;
        public byte[] ScryptRandomSalt;
        public byte[] Nonce;
        public bool ClearQuickPassOnIdle = true;
        public bool ClearQuickPassOnSleep = true;
        public bool ClearQuickPassOnSwitchingUser = true;
        public Timer Timer;
        public string IdentityUniqueId;
    }

    /// <summary>
    /// Represents reasons for clearing QuickPass-related data from RAM.
    /// </summary>
    public enum QuickPassClearReason
    {
        /// <summary>
        /// The system was in an idle state (no user input) for longer 
        /// than the IdleTimout setting.
        /// </summary>
        IdleTimeout,

        /// <summary>
        /// The QuickPass lifetime exceeded the app's general QuickPass
        /// timeout setting.
        /// </summary>
        QuickPassTimeout,

        /// <summary>
        /// The user session was locked, ended or was switched.
        /// </summary>
        UserSwitching,

        /// <summary>
        /// The system went into any form of blanking state, such as
        /// system standby, hilbernation or the screensaver kicked in.
        /// </summary>
        EnterBlankingState,

        /// <summary>
        /// The current SQRL identity was changed.
        /// </summary>
        IdentityChange,

        /// <summary>
        /// The reason for clearing the QuickPass is unspecified.
        /// </summary>
        Unspecified
    }

    /// <summary>
    /// Represents event arguments for the <c>QuickPassCleared</c> event.
    /// </summary>
    public class QuickPassClearedEventArgs : EventArgs
    {
        /// <summary>
        /// A list of identity unique ids for which the QuickPass has been cleared.
        /// </summary>
        public List<string> IdentityUniqueIds;

        public QuickPassClearedEventArgs(List<string> identityUniqueIds)
        {
            this.IdentityUniqueIds = identityUniqueIds;
        }
    }

    /// <summary>
    /// Encapsulates the SQRL block 1 keys which are held by
    /// a QuickPass entry.
    /// </summary>
    public class QuickPassDecryptedKeys
    {
        /// <summary>
        /// The Identity Master Key (IMK).
        /// </summary>
        public byte[] Imk;

        /// <summary>
        /// The Identity Lock Key (ILK).
        /// </summary>
        public byte[] Ilk;

        public QuickPassDecryptedKeys(byte[] imk, byte[] ilk)
        {
            this.Imk = imk;
            this.Ilk = ilk;
        }
    }
}
