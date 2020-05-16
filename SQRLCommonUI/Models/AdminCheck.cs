using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace SQRLCommonUI.Models
{
    /// <summary>
    /// This class allows you to check if the current application is being run as root / admin
    /// </summary>
    public class AdminCheck
    {
        [DllImport("libc")]
        private static extern uint getuid();

        /// <summary>
        /// Returns <c>true</c> if the current user has Administrator rights
        /// on Windows or is the root user on Linux/macOS.
        /// </summary>
        /// <returns></returns>
        public static bool IsAdmin()
        {
            bool isAdmin = false;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                isAdmin = getuid() == 0;
            else
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

                }
            }

            return isAdmin;
        }
    }
}
