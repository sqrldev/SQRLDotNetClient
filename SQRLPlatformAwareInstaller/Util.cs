using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace SQRLPlatformAwareInstaller
{
    public static class Utils
    {
        [DllImport("libc")]
        private static extern uint getuid();

        public static bool IsAdmin()
        {
            bool isAdmin = false;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                isAdmin= getuid() == 0;
            else
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    isAdmin= principal.IsInRole(WindowsBuiltInRole.Administrator);
                    
                }
            }

            return isAdmin;
        }
    }
}
