using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLPlatformAwareInstaller.Models
{
    /// <summary>
    /// Represents an interface for the actual client installation.
    /// 
    /// Platform-specific implementations for this interface can be found in 
    /// <c>SQRLPlatformAwareInstaller.Platform.XXX</c>.
    /// 
    /// </summary>
    interface IInstaller
    {
        /// <summary>
        /// Performs the actual installation if the client app.
        /// </summary>
        /// <param name="archiveFilePath">The full file name, including the path,
        /// of the archive containing the components to install.</param>
        public void Install(string archiveFilePath);

        /// <summary>
        /// Performs the actual installation if the client app.
        /// </summary>
        /// <param name="uninstallInfoFile">The full file name, including the path,
        /// of the file containing the uninstall information.</param>
        public void UnInstall(string uninstallInfoFile);
    }
}
