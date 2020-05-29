using Serilog;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using ToolBox.Bridge;

namespace SQRLCommon.Models
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
        /// Returns the home directory for the currently logged in user in Linux.
        /// </summary>
        /// <returns></returns>
        public static string GetHomePath()
        {
            string home = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                home = _shell.Term($"getent passwd { GetCurrentUser()} | cut -d: -f6", Output.Hidden).stdout.Trim();
            }

            return home;
        }


        /// <summary>
        /// Returns the currently logged on user name in Linux.
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentUser()
        {
            string user = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                user = _shell.Term("logname", Output.Hidden).stdout.Trim();
            }

            return user;
        }

        /// <summary>
        /// Checks to see if PolicyKit is installed and available in the system (Linux).
        /// </summary>
        /// <returns></returns>
        public static bool IsPolKitAvailable()
        {
            return !string.IsNullOrEmpty(GetPolKitLocation());
        }

        /// <summary>
        /// Gets the location of pkexec for policy kit on Linux.
        /// </summary>
        /// <returns></returns>
        public static string GetPolKitLocation()
        {
            string polKitLocation = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var result = _shell.Term("command -v pkexec", Output.Hidden);
                if (string.IsNullOrEmpty(result.stderr.Trim()) && !string.IsNullOrEmpty(result.stdout.Trim()))
                {
                    polKitLocation = result.stdout.Trim();
                }
            }
                
            return polKitLocation;
        }


        /// <summary>
        /// Excecutes a shell chmod command for the given path with the given permissions
        /// </summary>
        /// <param name="Path">Path of file to change permissions on</param>
        /// <param name="Permissions">Permissions of the file, defaulted to 755</param>
        /// <param name="Recursive">If true, the permission changes are applied recursiverlly</param>
        public static void Chmod(string Path, int Permissions=755, bool Recursive=false )
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Log.Information($"Changing permissions on file \"{Path}\" to \"{Permissions}\" (Recursive={Recursive})");
                _shell.Term($"chmod {(Recursive ? "-R " : "")}{Permissions} {Path}", Output.Hidden);
            }
        }

        /// <summary>
        /// Sets the executable bit for the file specified by <paramref name="filePath"/> on Linux.
        /// </summary>
        /// <param name="filePath">Path of the file to be set as executable.</param>
        public static void SetExecutableBit(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Log.Information($"Setting executable bit for file \"{filePath}\"");
                _shell.Term($"chmod a+x {filePath}", Output.Hidden);
            }
        }

        /// <summary>
        /// Tries launching the installer exectuable from %TEMP% using PolicyKit's "pkexec" command.
        /// </summary>
        /// <param name="args">The command line arguments to pass into the installer.</param>
        /// <param name="copyCurrentProcessExecutable">If set to <c>true</c>, the currently running process
        /// exectuable will be copied to the temp directory as the installer executable to run, if it isn't
        /// already running from there.</param>
        /// <returns>Returns <c>true</c> if the installer could be launched using PolicyKit, or <c>false</c> otherwise.</returns>
        public static bool LaunchInstallerUsingPolKit(string args = "", bool copyCurrentProcessExecutable = false)
        {
            Log.Information("Trying to launch installer using PolicyKit");

            // If PolicyKit is not available, there is no point in continuing.
            if (!IsPolKitAvailable())
            {
                Log.Warning("PolicyKit NOT available, bailing out");
                return false;
            }
            Log.Information("PolicyKit available, pkexec exists!");

            // Check if the policy file for the installer exists, this is needed for the polkit invokation
            if (!File.Exists(Path.Combine("/usr/share/polkit-1/actions", 
                "org.freedesktop.policykit.SQRLPlatformAwareInstaller_linux.policy")))
            {
                Log.Warning("PolicyKit policy file for installer not found, bailing out");
                return false;
            }
            Log.Information("Found existing PolicyKit policy file for Installer!");

            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
            string tempExePath = $"/tmp/{CommonUtils.GetInstallerByPlatform()}";

            if (copyCurrentProcessExecutable)
            {
                // Copy the current installer to /tmp/ so that it can comply with polkit requirements. Note this 
                // doesn't work correctly if you are in debug mode. In debug mode the file you are running is a dll, 
                // not an executable, so be mindful of this

                if (currentExePath != tempExePath)
                {
                    Log.Information($"Copying Installer from \"{currentExePath}\" to \"{tempExePath}\"");
                    File.Copy(currentExePath, tempExePath, true);
                    SystemAndShellUtils.Chmod(tempExePath, 777);
                }
            }

            // At this point, the installer exectuable must exist in the temp dir,
            // so if it doesn't, bail out.
            if (!File.Exists(tempExePath))
            {
                Log.Error($"Installer binary not found in \"{tempExePath}\", bailing out");
                return false;
            }

            // PolKit invocation forbids having a "dead" parent, so if we invoke PolKit directly from here
            // and then kill the process, it will abort. First we need to write the polkit invocation
            // to a shell script which is invoked externally, so that we can kill our current instance of 
            // the installer cleanly.

            Log.Information($"Creating PolicyKit launcher script");
            var tmpScript = Path.GetTempFileName().Replace(".tmp", ".sh");
            using (StreamWriter sw = new StreamWriter(tmpScript))
            {
                sw.WriteLine("#!/bin/sh");
                sw.WriteLine(string.IsNullOrWhiteSpace(args) ?
                    $"{SystemAndShellUtils.GetPolKitLocation()} {tempExePath}" :
                    $"{SystemAndShellUtils.GetPolKitLocation()} {tempExePath} {args}");
            }
            Log.Information($"Created PolicyKit launcher script at: {tmpScript}");
            SetExecutableBit(tmpScript);

            Log.Information($"Launching installer with args: {args}");
            Process proc = new Process();
            proc.StartInfo.FileName = tmpScript;
            proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(tmpScript);
            proc.Start();
            return true;
        }

        /// <summary>
        /// Tries launching the client as the specified user by using "sudo -i -u xxx".
        /// </summary>
        /// <param name="clientExePath">The full path to the client executable.</param>
        /// <param name="userName">The username to start the client with.</param>
        /// <returns>Returns <c>true</c> if the client could be launched, or <c>false</c> otherwise.</returns>
        public static bool LaunchClientAsUser(string clientExePath, string userName)
        {
            Log.Information($"Trying to launch client as user {userName}");

            if (!File.Exists(clientExePath))
            {
                Log.Error($"Client binary not found in \"{clientExePath}\", bailing out");
                return false;
            }

            Log.Information($"Creating client launcher script");
            var tmpScript = Path.GetTempFileName().Replace(".tmp", ".sh");
            using (StreamWriter sw = new StreamWriter(tmpScript))
            {
                sw.WriteLine("#!/bin/sh");
                sw.WriteLine($"sudo -i -u {userName} {clientExePath}");
            }
            Log.Information($"Created client launcher script at: {tmpScript}");
            SetExecutableBit(tmpScript);

            Log.Information($"Executing client launcher script");
            Process proc = new Process();
            proc.StartInfo.FileName = tmpScript;
            proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(tmpScript);
            proc.Start();
            return true;
        }
    }
}
