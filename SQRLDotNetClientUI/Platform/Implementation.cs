using Serilog;
using SQRLDotNetClientUI.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ToolBox.Bridge;

namespace SQRLDotNetClientUI.Platform
{
    /// <summary>
    /// Provides a central place for accessing platform-specific implementations.
    /// New platform-specific implementations should be registered here.
    /// </summary>
    public static class Implementation
    {
        private static IBridgeSystem _bridgeSystem { get; set; } = BridgeSystem.Bash;
        private static ShellConfigurator _shell { get; set; } = new ShellConfigurator(_bridgeSystem);

        /// <summary>
        /// Returns a <c>Type</c> that represents the platform-specific implementation for 
        /// the given type <typeparamref name="T"/>, or <c>null</c> if no implementation
        /// exists for the current platform.
        /// </summary>
        /// <typeparam name="T">The <c>Type</c> (mostly an interface type) to get a platform-
        /// specific implementation for.</typeparam>
        public static Type ForType<T>()
        {
            if (typeof(T) == typeof(INotifyIcon))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return typeof(Win.NotifyIcon);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // On Linux, we only support a tray icon for Ubuntu,
                    // sorry folks ¯\_(ツ)_/¯
                    var response = _shell.Term($"cat /etc/*-release", Output.Internal);
                    Log.Information($"Checking Linux distribution to see if we can use a tray icon");
                    Log.Information($"Output of \"cat / etc/*-release\" is:\r\n{response.stdout}");
                    if (!string.IsNullOrEmpty(response.stdout) && 
                        response.stdout.ToLower().Contains("ubuntu"))
                    {
                        return typeof(Linux.NotifyIcon);
                    }
                    return null;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return typeof(OSX.NotifyIcon);
                else return null;
            }

            if (typeof(T) == typeof(ISystemEventNotifier))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return typeof(Win.SystemEventNotifier);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return typeof(Linux.SystemEventNotifier);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return typeof(OSX.SystemEventNotifier);
                else return null;
            }

            throw new NotImplementedException(
                String.Format("No platform-specific implementations registered for type {0}!",
                typeof(T).ToString()));
        }
    }
}
