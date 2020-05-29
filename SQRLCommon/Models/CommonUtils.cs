using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Serilog;
using ToolBox.Bridge;
using System.Security.Principal;
using System.Security.AccessControl;

namespace SQRLCommon.Models
{
    public static class CommonUtils
    {
        private static ShellConfigurator _shell { get; set; } = new ShellConfigurator(BridgeSystem.Bash);

        /// <summary>
        /// Extracts the zip archive specified by <paramref name="archivePath"/> into the
        /// output directory <paramref name="outFolder"/> using <paramref name="password"/>.
        /// </summary>
        /// <param name="archivePath">The archive to extract.</param>
        /// <param name="password">The password for the archive.</param>
        /// <param name="outFolder">The output folder.</param>
        public static void ExtractZipFile(string archivePath, string password, string outFolder)
        {
            if (!Directory.Exists(outFolder))
            {
                Log.Information($"Creating folder \"{outFolder}\"");
                Directory.CreateDirectory(outFolder);
            }

            GrantFullFileAccess(outFolder);

            using (Stream fsInput = File.OpenRead(archivePath))
            {
                using (var zipFile = new ZipFile(fsInput))
                {
                    //We don't password protect our install but maybe we should
                    if (!String.IsNullOrEmpty(password))
                    {
                        // AES encrypted entries are handled automatically
                        zipFile.Password = password;
                    }

                    long fileCount = zipFile.Count;

                    foreach (ZipEntry zipEntry in zipFile)
                    {
                        if (!zipEntry.IsFile)
                        {
                            // Ignore directories
                            continue;
                        }

                        // Manipulate the output filename here as desired.
                        String entryFileName = zipEntry.Name;
                        var fullZipToPath = Path.Combine(outFolder, entryFileName);

                        //Do not over-write the sqrl Db if it exists
                        if (entryFileName.Equals(PathConf.DBNAME, StringComparison.OrdinalIgnoreCase) && File.Exists(fullZipToPath))
                        {
                            GrantFullFileAccess(fullZipToPath);
                            Log.Information("Found existing SQRL DB, keeping existing");

                            continue;
                        }

                        var directoryName = Path.GetDirectoryName(fullZipToPath);
                        if (directoryName.Length > 0 && directoryName != outFolder)
                        {
                            if (!Directory.Exists(directoryName))
                            {
                                Directory.CreateDirectory(directoryName);
                            }

                            GrantFullFileAccess(directoryName);
                        }

                        // 4K is optimum
                        var buffer = new byte[4096];

                        // Unzip file in buffered chunks. This is just as fast as unpacking
                        // to a buffer the full size of the file, but does not waste memory.
                        // The "using" will close the stream even if an exception occurs.
                        using (var zipStream = zipFile.GetInputStream(zipEntry))
                        using (Stream fsOutput = File.Create(fullZipToPath))
                        {
                            StreamUtils.Copy(zipStream, fsOutput, buffer);
                            GrantFullFileAccess(fullZipToPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extracts a single file specified by <paramref name="fileName"/> from the zip archive 
        /// specified by <paramref name="archivePath"/>, optionally using the specified <paramref name="password"/>,
        /// to the output file specified by <paramref name="outputFilePath"/>.
        /// </summary>
        /// <param name="archivePath">The full path to the archive to extract the file from.</param>
        /// <param name="password">The password for the archive. Can be <c>null</c>.</param>
        /// <param name="fileName">The file name (or path) within the archive of the file to extract.</param>
        /// <param name="outputFilePath">The full destination path for the extracted file.</param>
        public static void ExtractSingleFile(string archivePath, string password, string fileName, string outputFilePath)
        {
            using (Stream inputStream = File.OpenRead(archivePath))
            {
                using (var zipFile = new ZipFile(inputStream))
                {
                    //We don't password protect our install but maybe we should
                    if (!String.IsNullOrEmpty(password))
                    {
                        // AES encrypted entries are handled automatically
                        zipFile.Password = password;
                    }

                    ZipEntry zipEntry = zipFile.GetEntry(fileName);

                    // Check if the output directory exists and create it if necessary
                    var directoryName = Path.GetDirectoryName(outputFilePath);
                    if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                        GrantFullFileAccess(directoryName);
                    }

                    // 4K is optimum
                    var buffer = new byte[4096];

                    // Unzip file in buffered chunks. This is just as fast as unpacking
                    // to a buffer the full size of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (var zipStream = zipFile.GetInputStream(zipEntry))
                    using (Stream fsOutput = File.Create(outputFilePath))
                    {
                        StreamUtils.Copy(zipStream, fsOutput, buffer);
                        GrantFullFileAccess(outputFilePath);
                    }
                }
            }
        }

        /// <summary>
        /// Grants full access permissions for <paramref name="file"/> to the current user.
        /// </summary>
        /// <param name="file">The file to set the permissions for.</param>
        public static void GrantFullFileAccess(string file)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var fi = new FileInfo(file);
                var ac = fi.GetAccessControl();

                Log.Information($"Granting current user full file permissions to {file}");
                var fileAccessRule = new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl, AccessControlType.Allow);

                ac.AddAccessRule(fileAccessRule);
                fi.SetAccessControl(ac);

            }

        }
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
