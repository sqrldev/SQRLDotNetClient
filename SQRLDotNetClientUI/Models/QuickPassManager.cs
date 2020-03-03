using Sodium;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
        private object _dataSyncObj;
        private Dictionary<string, QuickPassInfo> _data = new Dictionary<string, QuickPassInfo>();

        /// <summary>
        /// Returns the singleton <c>QuickPassManager</c> instance. If 
        /// the instance does not exists yet, it will first be created.
        /// </summary>
        public static QuickPassManager Instance
        {
            get => _instance.Value;
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
        }

        ~QuickPassManager()
        {
            // Unregister the identity changed event handler
            _identityManager.IdentityChanged -= _identityManager_IdentityChanged;

            //Clear all QuickPass entries
            ClearAllQuickPass();
        }

        /// <summary>
        /// This event handler gets called if 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _identityManager_IdentityChanged(object sender, IdentityChangedEventArgs e)
        {
            if (ClearQuickPassOnIdentityChange)
            {
                ClearAllQuickPass();
            }
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
        /// Creates a QuickPass from the given <paramref name="password"/> and
        /// <paramref name="imk"/> using the QuickPass settings stored in 
        /// <paramref name="identity"/>, stores it in memory and establishes a 
        /// timer that will clear the QuickPass after the timeout set forth in
        /// <paramref name="identity"/>'s QuickPass settings.
        /// </summary>
        /// <param name="password">The full identity master password.</param>
        /// <param name="imk">The identity's unencrypted Identity Master Key (IMK).</param>
        /// <param name="identity">The identity that the QuickPass should be set for.</param>
        /// <param name="progress">An object implementing the IProgress interface for tracking the operation's progress (optional).</param>
        /// <param name="progressText">A string representing a text descrition for the progress indicator (optional).</param>
        public async void SetQuickPass(string password, byte[] imk, SQRLIdentity identity, IProgress<KeyValuePair<int, string>> progress = null, string progressText = null)
        {
            QuickPassInfo qpi = new QuickPassInfo()
            {
                EstablishedDate = DateTime.Now,
                QuickPassLength = identity.Block1.HintLength,
                IdentityUniqueId = identity.Block0.UniqueIdentifier.ToHex(),
                ScryptRandomSalt = SodiumCore.GetRandomBytes(16),
                InitVector = SodiumCore.GetRandomBytes(16),
                QuickPassTimeoutSecs = identity.Block1.PwdTimeoutMins * 60,
                Timer = new Timer()
            };

            qpi.Timer.Enabled = false;
            qpi.Timer.AutoReset = false; // Dont restart timer after calling elapsed
            qpi.Timer.Interval = qpi.QuickPassTimeoutSecs * 1000;
            qpi.Timer.Elapsed += _timer_Elapsed;

            string quickPass = password.Substring(0, qpi.QuickPassLength);
            KeyValuePair<int, byte[]> enscryptResult = await SQRL.EnScryptTime(quickPass, qpi.ScryptRandomSalt, (int)Math.Pow(2, 9), 1, progress, progressText);

            qpi.ScryptIterationCount = enscryptResult.Key;
            qpi.EncryptedImk = StreamEncryption.Encrypt(imk, qpi.InitVector, enscryptResult.Value);

            lock (_dataSyncObj)
            {
                _data.Add(qpi.IdentityUniqueId, qpi);
                qpi.Timer.Start();
            }
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
        public async Task<byte[]> GetQuickPassDecryptedImk(string quickPass, string identityUniqueId = null, IProgress<KeyValuePair<int, string>> progress = null, string progressText = null)
        {
            QuickPassInfo qpi = null;

            if (identityUniqueId == null)
                identityUniqueId = _identityManager.CurrentIdentity?.Block0?.UniqueIdentifier?.ToHex();

            if (identityUniqueId == null)
                throw new InvalidOperationException("Cannot return QuickPass-decrypted IMK without an identity!");
            
            lock (_dataSyncObj)
            {
                if (!_data.ContainsKey(identityUniqueId))
                    throw new ArgumentException("No QuickPass entry was found for the given identity unique id!");

                qpi = _data[identityUniqueId];
            }

            byte[] key = await SQRL.EnScryptCT(quickPass, qpi.ScryptRandomSalt, (int)Math.Pow(2, 9), qpi.ScryptIterationCount, progress, progressText);
            byte[] decryptedImk = StreamEncryption.Decrypt(qpi.EncryptedImk, qpi.InitVector, key);

            return decryptedImk;
        }

        /// <summary>
        /// Clears the QuickPass entries of all identities from memory. After calling 
        /// this method, SQRL will ask for the full master password again for all
        /// available identities.
        /// </summary>
        /// <param name="combineEvents">If set to <c>true</c>, <c>QuickPassManager</c>
        /// will not raise individual <c>QuickPassCleared</c> events for each of the
        /// QuickPass entries affected, but instead raise a combined event.</param>
        public void ClearAllQuickPass(bool combineEvents = true)
        {
            List<string> uniqueIdsAvailable = null;
            List<string> uniqueIdsCleared = new List<string>();

            lock (_dataSyncObj)
            {
                uniqueIdsAvailable = new List<string>(_data.Keys);
            }

            foreach (string uniqueId in uniqueIdsAvailable)
            {
                if (ClearQuickPass(uniqueId, !combineEvents))
                {
                    uniqueIdsCleared.Add(uniqueId);
                }
            }

            // Only fire a combined QuickPassCleared event if combining events is enabled 
            // and if any QuickPass entries have actually been cleared.
            if (combineEvents && uniqueIdsCleared.Count > 0)
            {
                QuickPassCleared?.Invoke(this, new QuickPassClearedEventArgs(uniqueIdsCleared));
            }
        }

        /// <summary>
        /// Clears the QuickPass entries of all identities from memory. After calling 
        /// this method, SQRL will ask for the full master password again for all
        /// available identities.
        /// </summary>
        /// <param name="identityUniqueId">The unique id (block 0) of the identity for 
        /// which to clear the QuickPass entry.</param>
        /// <param name="fireQuickPassClearedEvent">If set to <c>true</c>, a 
        /// <c>QuickPassCleared</c> event will be fired if the QuickPass for the given
        /// <paramref name="identityUniqueId"/> could be successfully cleared.</param>
        public bool ClearQuickPass(string identityUniqueId, bool fireClearedEvent = true)
        {
            lock (_dataSyncObj)
            {
                if (!_data.ContainsKey(identityUniqueId)) return false;
                var qpi = _data[identityUniqueId];

                // First, stop the QuickPass timer
                qpi.Timer.Stop();

                // Then, overwrite the encrypted imk so that we don't leave any
                // traces of key material in RAM.
                qpi.EncryptedImk.ZeroFill();

                // Finally, delete the QuickPass entry from our dictionary
                _data.Remove(identityUniqueId);
            }

            // Finally, fire the QuickPassCleared event
            if (fireClearedEvent) QuickPassCleared?.Invoke(this, new QuickPassClearedEventArgs(new List<string>() { identityUniqueId }));

            return true;
        }

        /// <summary>
        /// This event handler gets called when the QuickPass timer has elapsed,
        /// indicating that the QuickPass belonging to this specific timer shall be cleared.
        /// </summary>
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            QuickPassInfo qpi = null;
            Timer timer = (Timer)sender;

            // Get the QuickPassInfo that this Timer belongs to
            lock (_dataSyncObj)
            {
                foreach (var item in _data)
                {
                    if (timer == item.Value.Timer)
                    {
                        qpi = item.Value;
                        break;
                    }
                }
            }

            // If such a QuickPassInfo item exists, clear it
            if (qpi != null)
            {
                ClearQuickPass(qpi.IdentityUniqueId);
            }
        }

        /// <summary>
        /// This event gets fired when the QuickPass was cleared from memory.
        /// </summary>
        public EventHandler<QuickPassClearedEventArgs> QuickPassCleared;
    }

    /// <summary>
    /// This class acts as a container for QuickPass management information.
    /// </summary>
    public class QuickPassInfo
    {
        public byte[] EncryptedImk;
        public DateTime EstablishedDate;
        public int QuickPassLength;
        public int QuickPassTimeoutSecs;
        public int ScryptIterationCount;
        public byte[] ScryptRandomSalt;
        public byte[] InitVector;
        public Timer Timer;
        public string IdentityUniqueId;
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
}
