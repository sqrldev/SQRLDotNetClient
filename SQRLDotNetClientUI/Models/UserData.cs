using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.Models
{
    /// <summary>
    /// Stores user information such as the last loaded identity, etc.
    /// </summary>
    public class UserData
    {
        /// <summary>
        /// The database record id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The file path of the last loaded identity.
        /// </summary>
        public string LastLoadedIdentity { get; set; }
    }
}
