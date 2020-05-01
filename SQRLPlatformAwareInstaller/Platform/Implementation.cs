using SQRLPlatformAwareInstaller.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SQRLPlatformAwareInstaller.Platform
{
    /// <summary>
    /// Provides a central place for accessing platform-specific implementations.
    /// New platform-specific implementations should be registered here.
    /// </summary>
    class Implementation
    {
        /// <summary>
        /// Returns a <c>Type</c> that represents the platform-specific implementation for 
        /// the given type <typeparamref name="T"/>, or <c>null</c> if no implementation
        /// exists for the current platform.
        /// </summary>
        /// <typeparam name="T">The <c>Type</c> (mostly an interface type) to get a platform-
        /// specific implementation for.</typeparam>
        public static Type ForType<T>()
        {
            if (typeof(T) == typeof(IInstaller))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return typeof(Windows.Installer);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return typeof(Linux.Installer);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return typeof(OSX.Installer);
                else return null;
            }

            throw new NotImplementedException(
                String.Format("No platform-specific implementations registered for type {0}!",
                typeof(T).ToString()));
        }
    }
}
