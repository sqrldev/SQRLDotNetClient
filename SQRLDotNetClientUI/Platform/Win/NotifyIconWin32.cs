using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Serilog;
using Avalonia.Win32.Interop;
using SQRLDotNetClientUI.Models;
using System.Drawing;

namespace SQRLDotNetClientUI.Platform.Win
{
    /// <summary>
    /// Represents a "tray icon" on Windows.
    /// </summary>
    public class NotifyIconWin32 : INotifyIcon
    {
        private NativeWindow _nativeWindow = null;
        private readonly int _uID = 0;
        private static int _nextUID = 0;
        private bool _iconAdded = false;
        private string _iconPath = string.Empty;
        private Icon _icon = null;

        public event EventHandler<EventArgs> Click;
        public event EventHandler<EventArgs> DoubleClick;

        /// <summary>
        /// Gets or sets the icon for the notify icon.
        /// </summary>
        public string IconPath 
        {
            get => _iconPath;
            set
            {
                try
                {
                    _icon = new Icon(value);
                    _iconPath = value;
                }
                catch (Exception)
                {
                    _icon = null;
                    _iconPath = string.Empty;
                }

            }
        }

        /// <summary>
        /// Gets or sets the tooltip text for the notify icon.
        /// </summary>
        public string ToolTipText { get; set; }

        /// <summary>
        /// Gets or sets if the notify icon is visible or not.
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Creates a new nptify icon instance.
        /// </summary>
        public NotifyIconWin32()
        {
            _uID = ++_nextUID;
            _nativeWindow = new NativeWindow();
        }

        ~NotifyIconWin32()
        {
            UpdateIcon(remove: true);
        }

        /// <summary>
        /// Shows, hides or removes the notify icon based on the set properties and parameters.
        /// </summary>
        /// <param name="remove">If set to true, the notify icon will be removed.</param>
        private void UpdateIcon(bool remove = false)
        {
            UnmanagedMethods.NOTIFYICONDATA iconData = new UnmanagedMethods.NOTIFYICONDATA()
            {
                cbSize = Marshal.SizeOf(typeof(UnmanagedMethods.NOTIFYICONDATA)),
                hwnd = _nativeWindow.Handle,
                uID = _uID,
                uFlags = UnmanagedMethods.NIF_TIP | UnmanagedMethods.NIF_MESSAGE,
                uCallbackMessage = (int)UnmanagedMethods.CustomWindowsMessage.WM_TRAYMOUSE,
                hIcon = IntPtr.Zero,
                szTip = "Test"
            };

            if (!remove && _icon != null && Visible)
            {
                iconData.uFlags |= UnmanagedMethods.NIF_ICON;
                iconData.hIcon = _icon.Handle;

                if (!_iconAdded)
                {
                    UnmanagedMethods.Shell_NotifyIcon(UnmanagedMethods.NIM_ADD, ref iconData);
                    _iconAdded = true;
                }
                else
                {
                    UnmanagedMethods.Shell_NotifyIcon(UnmanagedMethods.NIM_ADD, ref iconData);
                }
            }
            else
            {
                UnmanagedMethods.Shell_NotifyIcon(UnmanagedMethods.NIM_DELETE, ref iconData);
                _iconAdded = false;
            }
        }

        public void Show()
        {
            Visible = true;
            UpdateIcon();
        }

        public void Hide()
        {
            Visible = false;
            UpdateIcon();
        }

        public void Remove()
        {
            UpdateIcon(remove: true);
        }
    }

    /// <summary>
    /// Represents a native Win32 window.
    /// </summary>
    public class NativeWindow
    {
        private UnmanagedMethods.WndProc _wndProc;
        private string _className = "NotIcoNatHelper";

        /// <summary>
        /// The Win32 window handle of the native window.
        /// </summary>
        public IntPtr Handle { get; set; }

        public NativeWindow()
        {
            _wndProc = new UnmanagedMethods.WndProc(WndProc);
            UnmanagedMethods.WNDCLASSEX wndClassEx = new UnmanagedMethods.WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<UnmanagedMethods.WNDCLASSEX>(),
                lpfnWndProc = _wndProc,
                hInstance = UnmanagedMethods.GetModuleHandle(null),
                lpszClassName = _className + Guid.NewGuid(),
            };

            ushort atom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);

            if (atom == 0)
            {
                throw new Win32Exception();
            }

            Handle = UnmanagedMethods.CreateWindowEx(0, atom, null, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            if (Handle == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            Log.Information("MyWndProc called: MSG = {Msg}", ((UnmanagedMethods.WindowsMessage)msg).ToString());
            switch (msg)
            {
                case (uint)UnmanagedMethods.WindowsMessage.WM_CLOSE:
                    UnmanagedMethods.DestroyWindow(hWnd);
                    break;
                case (uint)UnmanagedMethods.WindowsMessage.WM_DESTROY:
                    UnmanagedMethods.PostQuitMessage(0);
                    break;
                case (uint)UnmanagedMethods.CustomWindowsMessage.WM_TRAYMOUSE:
                    Log.Information("TRAYICON msg !!!!");
                    break;
                default:
                    return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
            }
            return IntPtr.Zero;
        }
    }
}
