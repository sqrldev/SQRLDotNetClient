using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.Platform.Linux
{
    public class LinuxTrayIcon: Eto.Forms.Form
    {
        public TrayIndicator _tray;
        private bool _startup = true;
        public LinuxTrayIcon()
        {
            
            ClientSize = new Size(200, 200);
            _tray = new TrayIndicator
            {
                Image = Eto.Drawing.Icon.FromResource("SQRLDotNetClientUI.Assets.sqrl_icon_normal_256_32_icon.ico"),
                Menu = new ContextMenu()
            };

            _tray.Show();
            _tray.Visible = true;
        }

        protected override void OnShown(EventArgs e)
        {
            if(_startup)
            {
                Visible=false;
            }
        }

        protected override void OnUnLoad(EventArgs e)
        {
            base.OnUnLoad(e);
            _tray.Hide();
        }

    }
}
