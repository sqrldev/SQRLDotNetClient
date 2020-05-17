using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using ToolBox.Bridge;

namespace SQRLCommonUI.Models
{
    /// <summary>
    /// This class allows you to check if the current application is being run as root / admin
    /// </summary>
    public class SystemAndShellUtils
    {
        private static IBridgeSystem _bridgeSystem { get; set; } = BridgeSystem.Bash;
        private static ShellConfigurator _shell { get; set; } = new ShellConfigurator(_bridgeSystem);

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

        /// <summary>
        /// Returns the Home Directory for a user in Linux
        /// </summary>
        /// <returns></returns>
        public static string GetHomePath()
        {
            string home = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && SystemAndShellUtils.IsAdmin())
            {
                string user = GetCurrentUser();
                home = _shell.Term($"getent passwd {user} | cut -d: -f6", Output.Hidden).stdout.Trim();
            }

            return home;
        }


        /// <summary>
        /// Returns the currently logged on user name in linux
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentUser()
        {
            string user = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && SystemAndShellUtils.IsAdmin())
            {
                user = _shell.Term("logname", Output.Hidden).stdout.Trim();
            }

            return user;
        }

        /// <summary>
        /// Checks to see if PolKit is installed and available in the system (linux)
        /// </summary>
        /// <returns></returns>
        public static bool IsPolKitAvailable()
        {
            bool pkInstalled = false;
            var result = _shell.Term("command -v pkexec", Output.Internal);
            if (string.IsNullOrEmpty(result.stderr.Trim()) && !string.IsNullOrEmpty(result.stdout))
            {
                pkInstalled = true;
            }
            return pkInstalled;
        }


        /// <summary>
        /// Excecutes a shell chmod command for the given path with the given permissions
        /// </summary>
        /// <param name="Path">Path of file to change permissions on</param>
        /// <param name="Permissions">Permissions of the file, defaulted to 755</param>
        /// <param name="Recursive">If true, the permission changes are applied recursiverlly</param>
        public static void Chmod(string Path, int Permissions=755, bool Recursive=false )
        {
            _shell.Term($"chmod {(Recursive ? "-R " : "")}{Permissions} {Path}",Output.Hidden);
        }

        /// <summary>
        /// Sets the given file as executable on linux
        /// </summary>
        /// <param name="Path">Path of the file to be set as executable</param>
        public static void SetExecutableBit(string Path)
        {
            _shell.Term($"chmod a+x {Path}", Output.Hidden);
        }
    }
}
