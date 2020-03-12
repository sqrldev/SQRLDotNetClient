﻿using Avalonia.Controls;
using SQRLDotNetClientUI.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MonoMac.AppKit;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Threading;

namespace SQRLDotNetClientUI.Platform.OSX
{
    public class NotifyIcon : INotifyIcon
    {
        private NSStatusItem _item;
        private NSStatusItem statusBarItem { get => _item; set {
                _item = value;
                UpdateMenu();
            } }// => AvaloniaLocator.Current.GetService<AppDelegate>()}

        public event EventHandler<EventArgs> Click;
        public event EventHandler<EventArgs> DoubleClick;
        public event EventHandler<EventArgs> RightClick;

        private void UpdateMenu()
        {
            if(_item!=null)
            {
                _item.ToolTip = this.ToolTipText;
                statusBarItem.Menu.RemoveAllItems();
                foreach (var x in _menu.Items.Cast<MenuItem>())
                {
                    NSMenuItem menuItem = new NSMenuItem(x.Header.ToString());
                    menuItem.Activated += (s, e) => { x.Command.Execute(null); };
                    statusBarItem.Menu.AddItem(menuItem);
                }
            }
        }

        private string _iconPath = "";
        public string IconPath { get => _iconPath; set => _iconPath = value; }
        private string _toolTip = "";
        public string ToolTipText { get => _toolTip; set { _toolTip = value; } }
        private ContextMenu _menu;
        public ContextMenu ContextMenu
        {
            get => _menu; set
            {
                _menu = value;
                UpdateMenu();
            }
        }
        public bool Visible { get; set; }

        public void Remove()
        {
            this.statusBarItem.Dispose();
        }

        public NotifyIcon()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var systemStatusBar = NSStatusBar.SystemStatusBar;
                statusBarItem = systemStatusBar.CreateStatusItem(30);
                var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                statusBarItem.Image = NSImage.FromStream(assets.Open(new Uri("resm:SQRLDotNetClientUI.Assets.SQRL_icon_normal_16.png")));
                statusBarItem.DoubleClick += (s, e) => { DoubleClick?.Invoke(this, new EventArgs()); };
                statusBarItem.ToolTip = this.ToolTipText;
                statusBarItem.Menu = new NSMenu();
            },DispatcherPriority.MaxValue);
        }
    }
}