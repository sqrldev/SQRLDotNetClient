using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Serilog;
using Avalonia.Win32.Interop;

namespace SQRLDotNetClientUI.Platform.Win
{
    /// <summary>
    /// Represents a "tray icon" on Windows.
    /// </summary>
    public class NotifyIconWin32
    {
        private NativeWindow _nativeWindow = null;
        private UnmanagedMethods.NOTIFYICONDATA _notifyIconData;
        private readonly int _uID = 0;
        private static int _nextUID = 0;
        private IntPtr _icon;

        public NotifyIconWin32(string iconPath)
        {
            _uID = ++_nextUID;

            _nativeWindow = new NativeWindow();
            _icon = UnmanagedMethods.LoadImage(IntPtr.Zero, iconPath, UnmanagedMethods.IMAGE_ICON, 0, 0,
                UnmanagedMethods.LR_DEFAULTSIZE | UnmanagedMethods.LR_LOADFROMFILE);

            _notifyIconData = new UnmanagedMethods.NOTIFYICONDATA()
            {
                cbSize = Marshal.SizeOf(typeof(UnmanagedMethods.NOTIFYICONDATA)),
                hwnd = _nativeWindow.Handle,
                uID = _uID,
                uFlags = UnmanagedMethods.NIF_TIP | UnmanagedMethods.NIF_ICON | UnmanagedMethods.NIF_MESSAGE,
                uCallbackMessage = (int)UnmanagedMethods.CustomWindowsMessage.WM_TRAYMOUSE,
                hIcon = _icon,
                szTip = "Test"
            };

            bool success = UnmanagedMethods.Shell_NotifyIcon(UnmanagedMethods.NIM_ADD, ref _notifyIconData);
        }

        ~NotifyIconWin32()
        {
            bool success = UnmanagedMethods.Shell_NotifyIcon(UnmanagedMethods.NIM_DELETE, ref _notifyIconData);
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
