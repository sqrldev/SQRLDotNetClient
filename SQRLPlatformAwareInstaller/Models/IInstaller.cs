using SQRLCommon.Models;
using System;
using System.Threading.Tasks;

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
        /// Performs the actual installation of the client app.
        /// </summary>
        /// <param name="archiveFilePath">The full file name, including the path,
        /// of the archive containing the components to install.</param>
        /// <param name="installPath">The full path to the client installation directory.</param>
        /// <param name="versionTag">The version number of the client to install.</param>
        public Task Install(string archiveFilePath, string installPath, string versionTag);

        /// <summary>
        /// Uninstalls the client app.
        /// </summary>
        /// <param name="progress">An object used for tracking the progress of the operation.</param>
        /// <param name="dryRun">If set to <c>true</c>, all uninstall operations are only simulated but not 
        /// actually performed. Used for testing.</param>
        public Task Uninstall(IProgress<Tuple<int, string>> progress = null, bool dryRun = true);

        /// <summary>
        /// Returns the full path to the client executable using the given <paramref name="installPath"/>.
        /// </summary>
        /// <param name="installPath">The client installation path.</param>
        public string GetClientExePath(string installPath);

        /// <summary>
        /// Returns the full path to the installer executable using the given <paramref name="installPath"/>.
        /// </summary>
        /// <param name="installPath">The client installation path.</param>
        public string GetInstallerExePath(string installPath);
    }
}
