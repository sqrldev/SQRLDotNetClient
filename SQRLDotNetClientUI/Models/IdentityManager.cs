using SQRLDotNetClientUI.DB.DBContext;
using SQRLDotNetClientUI.DB.Models;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        /// <summary>
        /// The constructor is private because <c>IdentityManager</c> 
        /// implements the singleton pattern. To get an instance, use 
        /// <c>IdentityManager.Instance</c> instead.
        /// </summary>
        private IdentityManager() 
        {
            _db = new SQRLDBContext();
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
                return GetIdentity(
                    GetUserData().LastLoadedIdentity);
            }
        }

        /// <summary>
        /// Imports a SQRL identity and stores it in the database.
        /// </summary>
        /// <param name="identity">The <c>SQRLIdentity</c> to be imported.</param>
        /// <param name="setAsCurrentIdentity">If set to <c>true</c>, the imported identity will be 
        /// set as the currently active identity after adding it to the database.</param>
        public void ImportIdentity(SQRLIdentity identity, bool setAsCurrentIdentity = true)
        {
            Identity newIdRec = new Identity();
            newIdRec.Name = identity.IdentityName;
            newIdRec.UniqueId = identity.Block0.UniqueIdentifier.ToHex();
            newIdRec.GenesisId = identity.Block0.GenesisIdentifier.ToHex();
            newIdRec.DataBytes = identity.ToByteArray();

            _db.Identities.Add(newIdRec);
            _db.SaveChanges();

            if (setAsCurrentIdentity)
            {
                UserData ud = GetUserData();
                ud.LastLoadedIdentity = newIdRec.UniqueId;
                _db.SaveChanges();
            }
        }

        /// <summary>
        /// Retrieves the <c>SQRLIdentity</c> with the given <paramref name="uniqueId"/>
        /// from the database.
        /// </summary>
        /// <param name="uniqueId">The unique id found in block type 0 of the identity to be returned.</param>
        public SQRLIdentity GetIdentity(string uniqueId)
        {
            if (_db.Identities.Count() < 1) return null;

            var id = _db.Identities
                .Single(i => i.UniqueId == uniqueId);

            return SQRLIdentity.FromByteArray(id.DataBytes, false);
        }

        private UserData GetUserData()
        {
            UserData result = null;
            
            result = _db.UserData.FirstOrDefault();
            if (result == null)
            {
                UserData ud = new UserData();
                ud.LastLoadedIdentity = string.Empty;
                _db.UserData.Add(ud);
                _db.SaveChanges();
                result = ud;
            }

            return result;
        }
    }
}
