using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.Platform.Linux
{
    public class LinuxTrayIcon: Eto.Forms.Form
    {
        private TrayIndicator _tray;

        public LinuxTrayIcon()
        {
            Title = "My Eto Form";
            ClientSize = new Size(200, 200);
            _tray = new TrayIndicator
            {
                //Image = Eto.Drawing.Icon.FromResource("EtoTrayApp.error.ico"),
                Menu = new ContextMenu
                {
                    Items =
               {
                  new ButtonMenuItem {Text = "About...", Command = new Command((s, e) =>
                  {
                     
                  })},
                  new ButtonMenuItem {Text = "Quit", Command = new Command((s, e) => Application.Instance.Quit())}
               }
                }
            };

            ShowInTaskbar = false;
            _tray.Visible = true;
        }

    }
}
