using Avalonia.Controls;
using System;
using System.Runtime.InteropServices;

namespace SQRLCommonUI.AvaloniaExtensions
{
    /// <summary>
    /// Provides extension methods for the Avalonia Window class, mainly
    /// providing workarounds or bugfixes.
    /// </summary>
    public static class WindowExtensions
    {
        private static readonly bool IsWin32NT = Environment.OSVersion.Platform == PlatformID.Win32NT;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Activates the window (brings it into the foreground). This is a bugfix
        /// for https://github.com/AvaloniaUI/Avalonia/issues/2975.
        /// </summary>
        /// <param name="window"></param>
        public static void ActivateWorkaround(this Window window)
        {
            if (ReferenceEquals(window, null)) throw new ArgumentNullException(nameof(window));

            // Call default Activate() anyway.
            window.Activate();

            // Skip workaround for non-windows platforms.
            if (!IsWin32NT) return;

            var platformImpl = window.PlatformImpl;
            if (ReferenceEquals(platformImpl, null)) return;

            var platformHandle = platformImpl.Handle;
            if (ReferenceEquals(platformHandle, null)) return;

            var handle = platformHandle.Handle;
            if (IntPtr.Zero == handle) return;

            try
            {
                SetForegroundWindow(handle);
            }
            catch {}
        }
    }
}
