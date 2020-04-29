using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Serilog;
using System.IO;
using System.Security.AccessControl;

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
                isAdmin= getuid() == 0;
            else
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    isAdmin= principal.IsInRole(WindowsBuiltInRole.Administrator);
                    
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
                using (var zf = new ZipFile(fsInput))
                {
                    //We don't password protect our install but maybe we should
                    if (!String.IsNullOrEmpty(password))
                    {
                        // AES encrypted entries are handled automatically
                        zf.Password = password;
                    }

                    long fileCt = zf.Count;

                    foreach (ZipEntry zipEntry in zf)
                    {

                        if (!zipEntry.IsFile)
                        {
                            // Ignore directories
                            continue;
                        }
                        String entryFileName = zipEntry.Name;


                        // Manipulate the output filename here as desired.
                        var fullZipToPath = Path.Combine(outFolder, entryFileName);
                        //Do not over-write the sqrl Db if it exists

                        if (entryFileName.Equals("sqrl.db", StringComparison.OrdinalIgnoreCase) && File.Exists(fullZipToPath))
                        {
                            GrantFullFileAccess(fullZipToPath);
                            Log.Information("Found existing SQRL DB , keeping existing");
                            continue;
                        }
                        var directoryName = Path.GetDirectoryName(fullZipToPath);
                        if (directoryName.Length > 0)
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
                        using (var zipStream = zf.GetInputStream(zipEntry))
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
    }
}
