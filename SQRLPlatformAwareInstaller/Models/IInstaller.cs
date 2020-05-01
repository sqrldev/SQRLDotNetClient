using GitHubApi;
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
        public Task Install(string archiveFilePath, string installPath);

        /// <summary>
        /// Performs the uninstallation of the client app.
        /// </summary>
        /// <param name="uninstallInfoFile">The full file name, including the path,
        /// of the file containing the uninstall information.</param>
        public Task Uninstall(string uninstallInfoFile);

        /// <summary>
        /// Returns the full path to the client executable using the given <paramref name="installPath"/>.
        /// </summary>
        /// <param name="installPath">The client installation path.</param>
        public string GetExecutablePath(string installPath);

        /// <summary>
        /// Returns download information for the asset corresponding to the current platform
        /// contained within <paramref name="selectedRelease"/>.
        /// </summary>
        /// <param name="selectedRelease">The selected Github release.</param>
        DownloadInfo GetDownloadInfoForAsset(GithubRelease selectedRelease);
    }

    /// <summary>
    /// Holds information about a downloadable asset within a Github release,
    /// such as download size and URL.
    /// </summary>
    public class DownloadInfo
    {
        /// <summary>
        /// Gets or sets the asset's download size.
        /// </summary>
        public decimal DownloadSize { get; set; } = 0;

        /// <summary>
        /// Gets or sets the asset's download URL.
        /// </summary>
        public string DownloadUrl { get; set; } = "";
    }
}
