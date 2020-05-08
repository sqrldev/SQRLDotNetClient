using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Serilog;
using System.IO;
using System.Security.AccessControl;
using Microsoft.Win32;
using System.Security.Cryptography;
using SQRLCommonUI.Models;

namespace SQRLPlatformAwareInstaller
{
    public static class Utils
    {
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
        /// Parses the provided registry key string and returns its base key.
        /// </summary>
        /// <param name="keyAsString">The string representation of the registry key.</param>
        public static RegistryKey GetRegistryBaseKey(string keyAsString)
        {
            var index = keyAsString.IndexOf("\\");
            if (index == -1) return null;

            string baseKeyStr = keyAsString.Substring(0, index);

            RegistryKey baseKey = null;

            switch (baseKeyStr)
            {
                case "HKEY_CLASSES_ROOT":
                    baseKey = Registry.ClassesRoot;
                    break;

                case "HKEY_CURRENT_USER":
                    baseKey = Registry.CurrentUser;
                    break;

                case "HKEY_LOCAL_MACHINE":
                    baseKey = Registry.LocalMachine;
                    break;

                case "HKEY_USERS":
                    baseKey = Registry.Users;
                    break;

                case "HKEY_PERFORMANCE_DATA":
                    baseKey = Registry.PerformanceData;
                    break;

                case "HKEY_CURRENT_CONFIG":
                    baseKey = Registry.CurrentConfig;
                    break;
            }

            return baseKey;
        }

        /// <summary>
        /// Parses the provided registry key string and returns its sub key
        /// (the part after the base key).
        /// </summary>
        /// <param name="keyAsString">The string representation of the registry key.</param>
        public static string GetRegistrySubKey(string keyAsString)
        {
            var index = keyAsString.IndexOf("\\");
            if (index == -1 || index == keyAsString.Length - 1) return null;

            return keyAsString.Substring(index + 1);
        }

        /// <summary>
        /// Generates the Hex Sha256 Hash of a File
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetFileHashSha256(string path)
        {
            SHA256 Sha256 = SHA256.Create();
            using (FileStream stream = File.OpenRead(path))
            {
                return String.Join(String.Empty, Array.ConvertAll(Sha256.ComputeHash(stream), x => x.ToString("X2")));
            }
        }



        /// <summary>
        /// Moves the Db from the current location to the new user space location
        /// </summary>
        /// <param name="currentPath"></param>
        /// <returns></returns>
        public static bool MoveDb(string currentPath)
        {
            Log.Information($"Attemting to move Db from: {currentPath} to: {PathConf.FullClientDbPath}");
            bool success = false;
            if (!Directory.Exists(PathConf.ClientDBPath))
            {
                Directory.CreateDirectory(PathConf.ClientDBPath);

                Utils.GrantFullFileAccess(PathConf.ClientDBPath);


            }

            if (!File.Exists(PathConf.FullClientDbPath))
            {
                File.Copy(currentPath, PathConf.FullClientDbPath, false);
                Utils.GrantFullFileAccess(PathConf.FullClientDbPath);
                if (Utils.GetFileHashSha256(PathConf.FullClientDbPath).Equals(Utils.GetFileHashSha256(currentPath)))
                {
                    File.Delete(currentPath);
                    Log.Information($"Successfully moved Db from {currentPath} to {PathConf.FullClientDbPath} ");
                    success = true;
                }
            }
            else
            {
                Log.Warning($"Tried to move the DB, but there is already a Db in place in {PathConf.FullClientDbPath}, not moving forward ");
                success = false;
            }

            return success;
        }

    }
}
