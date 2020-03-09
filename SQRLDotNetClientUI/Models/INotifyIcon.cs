using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.Models
{
    public interface INotifyIcon
    {
        /// <summary>
        /// Gets or sets the icon for the notify icon. Either a file system path
        /// or a <c>resm:</c> manifest resource path can be specified.
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// Gets or sets the tooltip text for the notify icon.
        /// </summary>
        public string ToolTipText { get; set; }

        /// <summary>
        /// Gets or sets if the notify icon is visible in the 
        /// taskbar notification area or not.
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Removes the notify icon from the taskbar notification area.
        /// </summary>
        public void Remove();

        public event EventHandler<EventArgs> Click;
        public event EventHandler<EventArgs> DoubleClick;
        public event EventHandler<EventArgs> RightClick;
    }
}
