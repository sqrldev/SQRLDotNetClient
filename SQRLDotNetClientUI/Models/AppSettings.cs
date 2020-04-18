using SQRLDotNetClientUI.DB.DBContext;
using SQRLDotNetClientUI.DB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQRLDotNetClientUI.Models
{
    /// <summary>
    /// This class exposes and manages application and user settings.
    /// </summary>
    public class AppSettings
    {
        private static AppSettings _instance = null;
        private SQRLDBContext _db = null;
        private UserData _userData = null;
        private bool _hasUnsavedChanges = false;

        /// <summary>
        /// Gets the singleton <c>AppSettings</c> instance.
        /// </summary>
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null) _instance = new AppSettings();
                return _instance;
            }
        }

        /// <summary>
        /// Gets a value indicating whether changes to the current app 
        /// settings were made that haven't been saved back to the database yet.
        /// </summary>
        public bool HasUnsavedChanges 
        {
            get { return _hasUnsavedChanges; }
        }

        /// <summary>
        /// The id of the last loaded identity.
        /// </summary>
        public string LastLoadedIdentity 
        { 
            get 
            { 
                return _userData.LastLoadedIdentity; 
            }
            set
            {
                _userData.LastLoadedIdentity = value;
                _hasUnsavedChanges = true;
            }
        }

        /// <summary>
        /// Gets or sets a value that determines if the app should start 
        /// minimized to the tray icon.
        /// </summary>
        public bool StartMinimized 
        {
            get
            {
                return _userData.StartMinimized;
            }
            set
            {
                _userData.StartMinimized = value;
                _hasUnsavedChanges = true;
            }
        }

        /// <summary>
        /// The constructor is private, use the <Instance>property</Instance>
        /// to get the singletion class instance.
        /// </summary>
        private AppSettings()
        {
            _db = new SQRLDBContext();
            _userData = GetUserData();
        }

        /// <summary>
        /// Saves any changed settings to the database.
        /// </summary>
        public void Save()
        {
            _db.SaveChanges();
        }

        /// <summary>
        /// Returns the <c>UserData</c> database entry. If no such entry
        /// exists, it will be created.
        /// </summary>
        private UserData GetUserData()
        {
            UserData result = null;

            result = _db.UserData.FirstOrDefault();
            if (result == null)
            {
                UserData ud = new UserData();
                ud.LastLoadedIdentity = string.Empty;
                ud.StartMinimized = false;
                _db.UserData.Add(ud);
                _db.SaveChanges();
                result = ud;
            }

            return result;
        }
    }
}
