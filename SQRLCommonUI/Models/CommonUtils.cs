using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SQRLCommonUI.Models
{
    public static class CommonUtils
    {
        /// <summary>
        /// Returns the name of the installer binary corresponding to the current platform.
        /// </summary>
        public static string GetInstallerByPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "SQRLPlatformAwareInstaller_win.exe";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "SQRLPlatformAwareInstaller_osx";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "SQRLPlatformAwareInstaller_linux";

            return "";
        }
    }
}
