using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.DB.Models
{
    /// <summary>
    /// Stores user information such as the last loaded identity, etc.
    /// </summary>
    public class UserData
    {
        /// <summary>
        /// The database record id for the <c>UserData</c>.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The id of the last loaded identity.
        /// </summary>
        public string LastLoadedIdentity { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if the app should start 
        /// minimized to the tray icon.
        /// </summary>
        public bool StartMinimized { get; set; } = false;
    }
}
