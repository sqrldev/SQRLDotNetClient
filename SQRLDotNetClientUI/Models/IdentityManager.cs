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
        private SQLiteDBContext _db;

        /// <summary>
        /// The constructor is private because <c>IdentityManager</c> 
        /// implements the singleton pattern. To get an instance, use 
        /// <c>IdentityManager.Instance</c> instead.
        /// </summary>
        private IdentityManager() 
        {
            _db = new SQLiteDBContext();
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
            //TODO: Impelement
            get;
        }

        /// <summary>
        /// Imports a SQRL identity by storing it in the database.
        /// </summary>
        /// <param name="identity">The <c>SQRLIdentity</c> to be imported.</param>
        public void ImportIdentity(SQRLIdentity identity)
        {
            //TODO: Impelement
        }

        /// <summary>
        /// Retrieves the <c>SQRLIdentity</c> with the given database <paramref name="id"/>
        /// from the sqlite database.
        /// </summary>
        /// <param name="id">The database record id of the identity to be returned.</param>
        public SQRLIdentity GetIdentity(int id)
        {
            //TODO: Impelement
            return null;
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
