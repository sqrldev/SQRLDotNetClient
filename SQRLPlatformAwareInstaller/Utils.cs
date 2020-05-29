using System;
using Serilog;
using System.IO;
using Microsoft.Win32;
using System.Security.Cryptography;
using SQRLCommon.Models;

namespace SQRLPlatformAwareInstaller
{
    public static class Utils
    {
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
            Log.Information($"Attemting to move DB from \"{currentPath}\" to \"{PathConf.FullClientDbPath}\"");
            bool success = false;
            if (!Directory.Exists(PathConf.ClientDBPath))
            {
                Directory.CreateDirectory(PathConf.ClientDBPath);
                CommonUtils.GrantFullFileAccess(PathConf.ClientDBPath);
            }

            if (!File.Exists(PathConf.FullClientDbPath))
            {
                File.Copy(currentPath, PathConf.FullClientDbPath, false);
                CommonUtils.GrantFullFileAccess(PathConf.FullClientDbPath);
                if (Utils.GetFileHashSha256(PathConf.FullClientDbPath).Equals(Utils.GetFileHashSha256(currentPath)))
                {
                    File.Delete(currentPath);
                    Log.Information($"Successfully moved Db from {currentPath} to {PathConf.FullClientDbPath} ");
                    success = true;
                }
            }
            else
            {
                Log.Warning($"Tried to move the DB, but there is already a DB in place in \"{PathConf.FullClientDbPath}\", not moving forward ");
                success = false;
            }

            return success;
        }
    }
}
