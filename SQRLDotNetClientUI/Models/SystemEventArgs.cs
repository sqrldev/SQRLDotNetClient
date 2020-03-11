using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.Models
{
    /// <summary>
    /// Represents event args for system event notifications.
    /// </summary>
    public class SystemEventArgs : EventArgs
    {
        /// <summary>
        /// The description of the system event that triggered the notification.
        /// </summary>
        public string EventDescription;

        public SystemEventArgs(string eventDescription)
        {
            this.EventDescription = eventDescription;
        }
    }
}
