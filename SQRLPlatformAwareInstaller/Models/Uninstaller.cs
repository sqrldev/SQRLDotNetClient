using Microsoft.Win32;
using Serilog;
using System;
using System.IO;
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
        /// <param name="dryRun">If set to <c>true</c>, all uninstall operations are only simulated but not 
        /// actually performed. Used for testing.</param>
        public static async Task Run(IProgress<Tuple<int, string>> progress = null, bool dryRun = true)
        {
            int operationCount = 0;

            Log.Information("Loading inventory");
            _inventory.Load();

            int totalOperationCount = _inventory.Data.Directories.Count +
                _inventory.Data.Files.Count + _inventory.Data.RegistryKeys.Count;

            Log.Information($"Running uninstaller, total operation count: {totalOperationCount}");

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
                        Log.Information($"{operationCount}/{totalOperationCount}: Deleting directory {dir}");
                        if (!dryRun) Directory.Delete(dir, true);
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
                        Log.Information($"{operationCount}/{totalOperationCount}: Deleting file {file}");
                        if (!dryRun) File.Delete(file);
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
                        Log.Information($"{operationCount}/{totalOperationCount}: Deleting registry key {regKey}");
                        RegistryKey baseKey = Utils.GetRegistryBaseKey(regKey);
                        if (baseKey == null) throw new ArgumentException($"Could not parse registry base key for {regKey}");
                        string subKey = Utils.GetRegistrySubKey(regKey);
                        if (!dryRun) baseKey.DeleteSubKeyTree(subKey, throwOnMissingSubKey: true);
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
    }
}
