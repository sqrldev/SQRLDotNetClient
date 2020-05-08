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

        private string _lastLoadedIdentity;
        private bool _startMinimized;

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
            get { return _lastLoadedIdentity; }
            set
            {
                if (_lastLoadedIdentity != value)
                {
                    _lastLoadedIdentity = value;
                    _hasUnsavedChanges = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that determines if the app should start 
        /// minimized to the tray icon.
        /// </summary>
        public bool StartMinimized 
        {
            get { return _startMinimized; }
            set
            {
                if (_startMinimized != value)
                {
                    _startMinimized = value;
                    _hasUnsavedChanges = true;
                }
            }
        }

        /// <summary>
        /// The constructor is private, use the <Instance>property</Instance>
        /// to get the singletion class instance.
        /// </summary>
        private AppSettings()
        {
            Initialize();
        }


        /// <summary>
        /// Called from various methods to load the DB and pull in default values.
        /// </summary>
        public void Initialize()
        {
            _db = SQRLDBContext.Instance;
            _userData = GetUserData();
            Reload();
        }

        /// <summary>
        /// Saves any changed settings to the database.
        /// </summary>
        public void Save()
        {
            if (!HasUnsavedChanges) return;

            _userData.LastLoadedIdentity = _lastLoadedIdentity;
            _userData.StartMinimized = _startMinimized;

            _db.SaveChanges();
        }

        /// <summary>
        /// Discards any usaved changes and reloads all settings
        /// from the database.
        /// </summary>
        public void Reload()
        {
            _lastLoadedIdentity = _userData.LastLoadedIdentity;
            _startMinimized = _userData.StartMinimized;

            _hasUnsavedChanges = false;
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
