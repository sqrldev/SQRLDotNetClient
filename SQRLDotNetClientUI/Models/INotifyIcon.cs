using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.Models
{
    public interface INotifyIcon
    {
        public void Show();
        public void Hide();
        public void Remove();
        
        public string IconPath { get; set; }
        public string ToolTipText { get; set; }

        public bool Visible { get; set; }

        public event EventHandler<EventArgs> Click;
        public event EventHandler<EventArgs> DoubleClick;
    }
}
