using Microsoft.Win32;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace SQRLPlatformAwareInstaller.Platform.Windows
{
    /// <summary>
    /// A helper class for detecting and installing the Microsoft
    /// Visual C++ Redistributable runtime on Windows.
    /// </summary>
    public static class VcRedistHelper
    {
        /// <summary>
        /// The direct download link to the Visual C++ Redistributable runtime executable.
        /// </summary>
        public static readonly string VcRedistDownloadLink = 
            "https://download.microsoft.com/download/9/3/F/93FCF1E7-E6A4-478B-96E7-D4B285925B00/vc_redist.x64.exe";

        /// <summary>
        /// Checks if the Visual C++ Redistributable runtime is installed on the system.
        /// </summary>
        public static bool IsRuntimeInstalled()
        {
            // First, we need to find the correct registry key to check for an existing
            // Visual C++ Redistributable runtime installation. This depends upong the 
            // bit-ness of the app and the OS.

            // We default to 64-bit process running on a 64-bit OS
            var vcRedistKeyStr = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64";

            if (!Environment.Is64BitProcess)
            {
                // We're running as a 32-bit process, check if the OS is 64 or 32 bits
                vcRedistKeyStr = Environment.Is64BitOperatingSystem ?
                    // 32-bit process on a 64-bit OS
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x86" :
                    // 32-bit process on a 32-bit OS
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86";
            }

            var vcRedistInstalled = Registry.GetValue(vcRedistKeyStr, "Installed", 0);

            return (vcRedistInstalled != null && (int)vcRedistInstalled == 1);
        }

        /// <summary>
        /// Tries to install the Visual C++ Redistributable runtime.
        /// </summary>
        /// <returns>Returns the exit code returned by the vc redistributable installer.</returns>
        public static int InstallRuntime()
        {
            WebClient wc = new WebClient();

            var downloadedFile = Path.Combine(Path.GetTempPath(),
                Environment.Is64BitProcess ? 
                "vc_redist.x64.exe" : 
                "vc_redist.x86.exe");

            try
            {
                wc.DownloadFile(VcRedistDownloadLink, downloadedFile);

                Process p = new Process();
                p.StartInfo.FileName = downloadedFile;
                p.StartInfo.Arguments = "/install /passive /norestart";
                p.StartInfo.UseShellExecute = true;
                p.Start();
                p.WaitForExit(1000 * 60 * 3); // 3 minutes

                return p.ExitCode;
            }
            catch (Exception ex)
            {
                Log.Error($"Error downloading or installing VC++ redistributable:\r\n{ex}");
                return -1;
            }
        }
    }
}
