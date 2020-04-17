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

        /// <summary>
        /// Creates a new <c>SystemEventArgs</c> instance.
        /// </summary>
        /// <param name="eventDescription">The description of the system event that triggered the notification.</param>
        public SystemEventArgs(string eventDescription)
        {
            this.EventDescription = eventDescription;
        }
    }
}
