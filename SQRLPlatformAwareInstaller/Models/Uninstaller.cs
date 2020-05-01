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
        /// <summary>
        /// Runs the actual uninstallation task.
        /// </summary>
        /// <param name="progress">An object used for tracking the progress of the operation.</param>
        /// <param name="dryRun">If set to <c>true</c>, all uninstall operations are only simulated but not 
        /// actually performed. Used for testing.</param>
        public static async Task Run(IProgress<Tuple<int, string>> progress = null, bool dryRun = true)
        {

            Log.Information("Loading inventory");
            Inventory inventory = Inventory.Instance; 
            inventory.Load();

            int currentItem = 0;
            int totalItems = inventory.InventoryItemCount;

            Log.Information($"Running uninstaller, total operation count: {totalItems}");

            if (totalItems == 0)
            {
                Log.Information($"Nothing to do, aborting uninstallation.");
                progress.Report(new Tuple<int, string>(100, $"Nothing to do, aborting uninstallation."));
                return;
            }

            await Task.Run(() =>
            {
                // Remove directories
                foreach (var dir in inventory.Data.Directories)
                {
                    currentItem++;

                    try
                    {
                        Log.Information($"{currentItem}/{totalItems}: Deleting directory {dir}");
                        if (!dryRun) Directory.Delete(dir, true);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Error deleting directory {dir}:\r\n{ex.Message}");
                    }
                    finally
                    {
                        progress.Report(new Tuple<int, string>(
                            (int)(100 / totalItems * currentItem), $"Deleting directory {dir}"));
                    }
                }

                // Remove single files
                foreach (var file in inventory.Data.Files)
                {
                    currentItem++;

                    try
                    {
                        Log.Information($"{currentItem}/{totalItems}: Deleting file {file}");
                        if (!dryRun) File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Error deleting file {file}:\r\n{ex.Message}");
                    }
                    finally
                    {
                        progress.Report(new Tuple<int, string>(
                            (int)(100 / totalItems * currentItem), $"Deleting file {file}"));
                    }
                }

                // Remove registry keys
                foreach (var regKey in inventory.Data.RegistryKeys)
                {
                    currentItem++;

                    try
                    {
                        Log.Information($"{currentItem}/{totalItems}: Deleting registry key {regKey}");
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
                        (int)(100 / totalItems * currentItem), $"Deleting registry key {regKey}"));
                    }
                }
            });
        }
    }
}
