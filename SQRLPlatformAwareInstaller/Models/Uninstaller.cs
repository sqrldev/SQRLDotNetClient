using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SQRLPlatformAwareInstaller.Models
{
    /// <summary>
    /// Provides functionality to perform the actual task of uninstalling
    /// the app's components from the system by reading the install inventory 
    /// and subsequently removing all the recorded items.
    /// </summary>
    public static class Uninstaller
    {
        private static Inventory _inventory = Inventory.Instance;

        /// <summary>
        /// Runs the actual uninstallation task.
        /// </summary>
        /// <param name="progress">An object used for tracking the progress of the operation.</param>
        public static async Task Run(IProgress<Tuple<int, string>> progress)
        {
            int operationCount = 0;
            int totalOperationCount = _inventory.Data.Directories.Count +
                _inventory.Data.Files.Count + _inventory.Data.RegistryKeys.Count;

            Log.Information($"Running uninstaller, total operation count: {operationCount}");

            if (totalOperationCount == 0)
            {
                Log.Information($"Nothing to do, aborting uninstallation.");
                return;
            }

            await Task.Run(() =>
            {
                // Remove directories
                foreach (var dir in _inventory.Data.Directories)
                {
                    operationCount++;

                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Error deleting directory {dir}:\r\n{ex.Message}");
                    }
                    finally
                    {
                        progress.Report(new Tuple<int, string>(
                            (int)(100 / totalOperationCount * operationCount), $"Deleting directory {dir}"));
                    }
                }

                // Remove single files
                foreach (var file in _inventory.Data.Files)
                {
                    operationCount++;

                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Error deleting file {file}:\r\n{ex.Message}");
                    }
                    finally
                    {
                        progress.Report(new Tuple<int, string>(
                            (int)(100 / totalOperationCount * operationCount), $"Deleting file {file}"));
                    }
                }

                // Remove registry keys
                foreach (var regKey in _inventory.Data.RegistryKeys)
                {
                    operationCount++;

                    try
                    {
                        RegistryKey key = ParseRegistryKey(regKey);
                        if (key == null) throw new ArgumentException($"Could not parse registry key {regKey}");
                        //TODO!
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Error deleting registry key {regKey}:\r\n{ex.Message}");
                    }
                    finally
                    {
                        progress.Report(new Tuple<int, string>(
                        (int)(100 / totalOperationCount * operationCount), $"Deleting registry key {regKey}"));
                    }
                }
            });
        }

        public static RegistryKey ParseRegistryKey(string keyAsString, bool openForWriting = true)
        {
            var index = keyAsString.IndexOf("\\\\");
            if (index == -1) return null;

            string baseKeyStr = keyAsString.Substring(0, index);
            string subKeyStr = keyAsString.Substring(index);

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

            if (baseKey == null) return null;

            return baseKey.OpenSubKey(subKeyStr, openForWriting);
        }
    }
}
