using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLPlatformAwareInstaller.Models
{
    /// <summary>
    /// An interface for performing the actual client installation/uninstallation.
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
        /// <param name="installPath">The full path to the client installation directory.</param>
        /// <param name="progress">An object for receiving installation progress updates.</param>
        public void Install(string archiveFilePath, string installPath, IProgress<int> progress);

        /// <summary>
        /// Performs the uninstallation if the client app.
        /// </summary>
        /// <param name="uninstallInfoFile">The full file name, including the path,
        /// of the file containing the uninstall information.</param>
        public void UnInstall(string uninstallInfoFile);

        /// <summary>
        /// Returns the full path to the client executable using the given
        /// <paramref name="installPath"/>.
        /// </summary>
        /// <param name="installPath">The client installation path.</param>
        public string GetExecutablePath(string installPath);
    }
}
