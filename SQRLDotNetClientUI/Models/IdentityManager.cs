using SQRLDotNetClientUI.DB.DBContext;
using SQRLDotNetClientUI.DB.Models;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Serilog;

namespace SQRLDotNetClientUI.Models
{
    /// <summary>
    /// Provides functionality for storing, retrieving and
    /// managing SQRL identities.
    /// </summary>
    public sealed class IdentityManager
    {
        private static readonly Lazy<IdentityManager> _instance = new Lazy<IdentityManager>(() => new IdentityManager());
        private SQRLDBContext _db;
        private AppSettings _appSettings = AppSettings.Instance;
        private Identity _currentIdentityDB = null;
        private SQRLIdentity _currentIdentity = null;
        private ILogger _log = Log.ForContext(typeof(IdentityManager));

        /// <summary>
        /// The constructor is private because <c>IdentityManager</c> 
        /// implements the singleton pattern. To get an instance, use 
        /// <c>IdentityManager.Instance</c> instead.
        /// </summary>
        private IdentityManager()
        {
            _db = new SQRLDBContext();
            _currentIdentityDB = GetIdentityInternal(_appSettings.LastLoadedIdentity);
            if (_currentIdentityDB != null) _currentIdentity = DeserializeIdentity(_currentIdentityDB.DataBytes);

            _log.Information("IdentityManager initialized.");
        }

        /// <summary>
        /// Returns the singleton <c>IdentityManager</c> instance. If 
        /// the instance does not exists yet, it will first be created.
        /// </summary>
        public static IdentityManager Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        /// <summary>
        /// Returns the currently active identity.
        /// </summary>
        public SQRLIdentity CurrentIdentity
        {
            get
            {
                return _currentIdentity;
            }
        }

        /// <summary>
        /// Returns the currently active identity.
        /// </summary>
        public string CurrentIdentityUniqueId
        {
            get
            {
                return _currentIdentityDB?.UniqueId;
            }
        }

        /// <summary>
        /// Returns the count of identities currently stored in the database
        /// </summary>
        public int IdentityCount
        {
            get
            {
                return _db.Identities.Count();
            }
        }

        /// <summary>
        /// Tries setting the identity with the given <paramref name="uniqueId"/> as
        /// the currently active identity. If the currently selected identity should
        /// be unspecified, just pass <c>null</c> for the <paramref name="uniqueId"/>.
        /// </summary>
        /// <param name="uniqueId">The unique id of the identity to be set active.</param>
        public void SetCurrentIdentity(string uniqueId)
        {
            if (_currentIdentityDB?.UniqueId == uniqueId) return;

            Identity id = null;

            if (uniqueId != null)
            {
                // Fetch the identity from the database
                id = GetIdentityInternal(uniqueId);
                if (id == null) throw new ArgumentException("No matching identity found!", nameof(uniqueId));

                // Set it as currently active identity
                _currentIdentityDB = id;
                _currentIdentity = DeserializeIdentity(_currentIdentityDB.DataBytes);

                // Save the last active identity unique id in the database
                _appSettings.LastLoadedIdentity = id.UniqueId;
                _appSettings.Save();
            }
            else
            {
                _currentIdentityDB = null;
                _currentIdentity = null;
            }

            // And finally fire the IdentityChanged event
            IdentityChanged?.Invoke(this, new IdentityChangedEventArgs(
                _currentIdentity,
                (id != null) ? id.Name : "",
                (id != null) ? id.UniqueId : ""));
        }

        /// <summary>
        /// Writes changes made to the given <paramref name="identity"/> back to the database.
        /// </summary>
        /// <param name="identity">The changed identity which should be updated in the database.</param>
        public void UpdateIdentity(SQRLIdentity identity)
        {
            Identity id = GetIdentityInternal(identity.Block0?.UniqueIdentifier.ToHex());
            if (id == null) throw new ArgumentException("This identity does not exist!", nameof(identity));

            id.DataBytes = SerializeIdentity(identity);
            _db.SaveChanges();
        }

        /// <summary>
        /// Writes changes made to the currently active <c>SQRLIdentity</c> back to the database.
        /// </summary>
        public void UpdateCurrentIdentity()
        {
            _currentIdentityDB.DataBytes = SerializeIdentity(_currentIdentity);
            _db.SaveChanges();
        }

        /// <summary>
        /// Deletes the currently active <c>SQRLIdentity</c> from the database.
        /// </summary>
        public void DeleteCurrentIdentity()
        {
            _db.Identities.Remove(_currentIdentityDB);
            _db.SaveChanges();

            if (_db.Identities.Count() >= 1)
            {
                Identity id = _db.Identities.First();
                SetCurrentIdentity(id.UniqueId);
            }
            else
            {
                SetCurrentIdentity(null);
            }

            // Fire the IdentityCountChanged event
            IdentityCountChanged?.Invoke(this, new IdentityCountChangedEventArgs(this.IdentityCount));
        }

        /// <summary>
        /// Imports a SQRL identity and stores it in the database.
        /// </summary>
        /// <param name="identity">The <c>SQRLIdentity</c> to be imported.</param>
        /// <param name="setAsCurrentIdentity">If set to <c>true</c>, the imported identity will be 
        /// set as the currently active identity after adding it to the database.</param>
        public void ImportIdentity(SQRLIdentity identity, bool setAsCurrentIdentity = true)
        {
            if (identity.Block0 == null)
            {
                throw new InvalidOperationException("The identity does not contain a type 0 block!");
            }

            if (HasIdentity(identity.Block0.UniqueIdentifier.ToHex()))
            {
                throw new InvalidOperationException("The identity already exists in the database!");
            }

            Identity newIdRec = new Identity();
            newIdRec.Name = identity.IdentityName;
            newIdRec.UniqueId = identity.Block0.UniqueIdentifier.ToHex();
            newIdRec.GenesisId = identity.Block0.GenesisIdentifier.ToHex();

            // Serialize the identity for storing it in the database.
            // We could use identity.ToByteArray() here, but then we would
            // lose extra information not covered by the S4 format, such
            // as identity name, file path etc.
            newIdRec.DataBytes = SerializeIdentity(identity);

            _db.Identities.Add(newIdRec);
            _db.SaveChanges();

            if (setAsCurrentIdentity)
            {
                SetCurrentIdentity(newIdRec.UniqueId);
            }

            // Finally, fire the IdentityCountChanged event
            IdentityCountChanged?.Invoke(this, new IdentityCountChangedEventArgs(this.IdentityCount));
        }

        /// <summary>
        /// Checks if a <c>SQRLIdentity</c> with the given <paramref name="uniqueId"/>
        /// is already present in the database.
        /// </summary>
        /// <param name="uniqueId">The unique id of the identity to check for.</param>
        public bool HasIdentity(string uniqueId)
        {
            return _db.Identities.Where(x => x.UniqueId == uniqueId).Count() > 0;
        }

        /// <summary>
        /// Retrieves the <c>SQRLIdentity</c> with the given <paramref name="uniqueId"/>
        /// from the database. If no such identity was found, the method returns <c>null</c>.
        /// </summary>
        /// <param name="uniqueId">The unique id of the identity to be returned.</param>
        public SQRLIdentity GetIdentity(string uniqueId)
        {
            Identity id = GetIdentityInternal(uniqueId);
            if (id == null) return null;

            return DeserializeIdentity(id.DataBytes);
        }

        /// <summary>
        /// Returns a list of available identities (name and unique id).
        /// </summary>
        /// <returns>A list of <c>Tuple</c>s, each containing two strings, where the first 
        /// one is the identity's name and the second is the identity's unique id.</returns>
        public List<Tuple<string, string>> GetIdentities()
        {
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();

            foreach (var id in _db.Identities)
                result.Add(new Tuple<string, string>(id.Name, id.UniqueId));

            return result;
        }

        /// <summary>
        /// Retrieves the <c>Identity</c> with the given <paramref name="uniqueId"/>
        /// from the database. If no such identity was found, the method returns <c>null</c>.
        /// </summary>
        /// <param name="uniqueId">The unique id of the identity to be returned.</param>
        private Identity GetIdentityInternal(string uniqueId)
        {
            if (_db.Identities.Count() < 1) return null;

            return _db.Identities
                .Single(i => i.UniqueId == uniqueId);
        }

        /// <summary>
        /// Serializes a <c>SQRLIdentity</c> object to a byte array.
        /// </summary>
        /// <param name="identity">The identity to be serialized.</param>
        private byte[] SerializeIdentity(SQRLIdentity identity)
        {
            byte[] identityBtes;
            IFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, identity);
                identityBtes = stream.ToArray();
            }
            return identityBtes;
        }

        /// <summary>
        /// Deserializes a <c>SQRLIdentity</c> object from a byte array.
        /// </summary>
        /// <param name="dataBytes">The byte array representing the serialized <c>SQRLIdentity</c>.</param>
        private SQRLIdentity DeserializeIdentity(byte[] dataBytes)
        {
            IFormatter formatter = new BinaryFormatter();
            return (SQRLIdentity)formatter.Deserialize(new MemoryStream(dataBytes));
        }

        /// <summary>
        /// This event is raised if the currently selected identity changes.
        /// </summary>
        public event EventHandler<IdentityChangedEventArgs> IdentityChanged;

        /// <summary>
        /// This event is raised if the number of available identities changes.
        /// </summary>
        public event EventHandler<IdentityCountChangedEventArgs> IdentityCountChanged;
    }

    public class IdentityChangedEventArgs : EventArgs
    {
        public SQRLIdentity Identity { get; }
        public string IdentityName { get; }
        public string IdentityUniqueId { get; }

        public IdentityChangedEventArgs(SQRLIdentity identity, string identityName, string identityUniqueId)
        {
            this.Identity = identity;
            this.IdentityName = identityName;
            this.IdentityUniqueId = identityUniqueId;
        }
    }

    public class IdentityCountChangedEventArgs : EventArgs
    {
        public int IdentityCount { get; }

        public IdentityCountChangedEventArgs(int identityCount)
        {
            this.IdentityCount = identityCount;
        }
    }
}