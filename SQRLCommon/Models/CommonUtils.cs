using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SQRLCommon.Models
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

        /// <summary>
        /// Returns the download link for the installation archive of the given <paramref name="release"/>.
        /// </summary>
        /// <param name="release">The release for which the download link should be returned.</param>
        public static string GetDownloadLinkByPlatform(GithubRelease release)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return release.assets.Where(x => x.name.Contains("win-x64.zip")).First().browser_download_url;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return release.assets.Where(x => x.name.Contains("osx-x64.zip")).First().browser_download_url;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return release.assets.Where(x => x.name.Contains("linux-x64.zip")).First().browser_download_url;

            return "";
        }

        /// <summary>
        /// Returns the full file name, including the path, of the file containing
        /// the latest release information downloaded from Github.
        /// </summary>
        public static string GetReleasesFilePath()
        {
            return Path.Combine(Path.GetTempPath(), "sqrl_releases.json");
        }
    }
}
