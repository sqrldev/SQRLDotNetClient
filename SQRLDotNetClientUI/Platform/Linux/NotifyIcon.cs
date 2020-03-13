using Avalonia.Controls;
using Eto.Forms;
using SQRLDotNetClientUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
namespace SQRLDotNetClientUI.Platform.Linux
{
    public class NotifyIcon : INotifyIcon
    {

        public event EventHandler<EventArgs> Click;
        public event EventHandler<EventArgs> DoubleClick;
        public event EventHandler<EventArgs> RightClick;
        private LinuxTrayIcon lti;
        private LinuxTrayIcon ltiProp { get => lti; set { lti=value; UpdateMenu();}}
        private System.Threading.CancellationTokenSource canTok;
        /// <summary>
        /// Gets or sets the icon for the notify icon. Either a file system path
        /// or a <c>resm:</c> manifest resource path can be specified.
        /// </summary>
        private string _iconPath = "";
        public string IconPath { get => _iconPath; set { _iconPath = value; UpdateMenu(); } }
        private string _toolTip = "";

        /// <summary>
        /// Gets or sets the tooltip text for the notify icon.
        /// </summary>
        public string ToolTipText { get => _toolTip; set { _toolTip = value; UpdateMenu(); } }
        private Avalonia.Controls.ContextMenu _menu;
        /// <summary>
        /// Gets or sets the context menu for the notify icon.
        /// </summary>
        public Avalonia.Controls.ContextMenu ContextMenu
        {
            get => _menu; set
            {
                _menu = value;
                UpdateMenu();
            }
        }

        /// <summary>
        /// Gets or sets if the notify icon is visible in the 
        /// taskbar notification area or not.
        /// </summary>
        public bool Visible { get; set; }
        

        public void Remove()
        {
            lti._tray.Hide();
            canTok.Cancel();
        }

        private void UpdateMenu()
        {
            if(lti!=null)
            {
                //lti._tray.Image = ;
                 Dispatcher.UIThread.Post(() =>
                {
                    lti._tray.Image=Eto.Drawing.Icon.FromResource(IconPath.Replace("resm:", ""));
                    lti._tray.Title = this.ToolTipText;
                    lti._tray.Menu=null;
                    var ctxMnu = new Eto.Forms.ContextMenu();
                    foreach (var x in _menu.Items.Cast<Avalonia.Controls.MenuItem>())
                    {
                        ButtonMenuItem bmi = new ButtonMenuItem();
                        bmi.Text = x.Header.ToString();
                        bmi.Command = new Command((s, e) => { Dispatcher.UIThread.Post(() =>
                                                    {
                                                        x.Command.Execute(null);
                                                    }); 
                                                });
                        ctxMnu.Items.Add(bmi);
                    }
                    lti._tray.Menu = ctxMnu;
                    lti._tray.Activated += (s, e) => { this.Click?.Invoke(this, new EventArgs()); };
                    
                    
                },DispatcherPriority.MaxValue);
            }
          

        }

        public NotifyIcon()
        {
            canTok = new System.Threading.CancellationTokenSource();
            Task.Factory.StartNew(() =>
                {
                    new Eto.Forms.Application(Eto.Platform.Detect).Run(ltiProp = new LinuxTrayIcon());
                }
            , canTok.Token,TaskCreationOptions.AttachedToParent, TaskScheduler.Default);
            UpdateMenu();
            
        }
    }
}
