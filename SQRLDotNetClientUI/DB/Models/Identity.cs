using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SQRLDotNetClientUI.DB.Models
{
    /// <summary>
    /// This is a database model representing a SQRL identity record.
    /// </summary>
    public class Identity
    {
        /// <summary>
        /// The unique identifier of the identity.
        /// </summary>
        [Key]
        public string UniqueId { get; set; }

        /// <summary>
        /// The genesis identifier of the identity.
        /// </summary>
        public string GenesisId { get; set; }

        /// <summary>
        /// The identity's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The identity's raw data bytes.
        /// </summary>
        public byte[] DataBytes { get; set; }
    }
}
