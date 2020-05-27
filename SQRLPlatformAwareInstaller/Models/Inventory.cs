using Serilog;
using SQRLCommon.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SQRLPlatformAwareInstaller.Models
{
    /// <summary>
    /// Manages an "inventory" of files, directories and registry entries 
    /// created by the installer during the installation procedure. This
    /// inventory will be used to support uninstalling the product.
    /// </summary>
    public class Inventory
    {
        private static Inventory _instance = null;
        
        /// <summary>
        /// The file name of json file representing the install inventory.
        /// </summary>
        public readonly string InventoryFile = Path.Combine(
            PathConf.ClientInstallPath, "install_inventory.json");

        /// <summary>
        /// Gets the current inventory data.
        /// </summary>
        public InventoryModel Data { get; internal set; } = new InventoryModel();

        /// <summary>
        /// Gets the total count of all inventory items.
        /// </summary>
        public int InventoryItemCount
        {
            get
            {
                return this.Data.Directories.Count + this.Data.Files.Count + 
                    this.Data.RegistryKeys.Count;
            }
        }

        /// <summary>
        /// Gets the singleton instance for this class.
        /// </summary>
        public static Inventory Instance
        {
            get
            {
                if (_instance == null)
                {
                    Log.Information("Creating singleton Inventory instance");
                    _instance = new Inventory();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private contstructor. Use <c>Inventory.Instance</c> to get the
        /// singleton instance for this class.
        /// </summary>
        private Inventory() { }

        /// <summary>
        /// Loads an existing inventory from file. If the file does not exist,
        /// an empty Inventory will be loaded.
        /// </summary>
        public void Load()
        {
            Log.Information($"Loading inventory");

            if (!File.Exists(InventoryFile))
            {
                Log.Information($"No inventory file found, continuing with empty inventory");
                this.Data = new InventoryModel();
            }
            else
            {
                Log.Information($"Inventory file found, loading");
                var json = File.ReadAllText(InventoryFile);
                this.Data = JsonSerializer.Deserialize<InventoryModel>(json);
            }

            Log.Information($"Inventory contains {this.InventoryItemCount} items");
        }

        /// <summary>
        /// Saves the curent inventory state to the json file.
        /// </summary>
        public void Save()
        {
            Log.Information($"Saving inventory with {this.InventoryItemCount} items");

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(this.Data, this.Data.GetType(), options);
            File.WriteAllText(InventoryFile, json);
        }

        /// <summary>
        /// Adds a directory path to the inventory. This addition will not be
        /// saved back to file unil <c>Inventory.Save()</c> is getting called.
        /// </summary>
        /// <param name="directory">The full path of the directory to add.</param>
        public void AddDirectory(string directory)
        {
            Log.Information($"Adding directory \"{directory}\" to inventory");
            Data.Directories.Add(directory);
        }

        /// <summary>
        /// Adds a file path to the inventory. This addition will not be
        /// saved back to file unil <c>Inventory.Save()</c> is getting called.
        /// </summary>
        /// <param name="file">The full path of the file to add.</param>
        public void AddFile(string file)
        {
            Log.Information($"Adding file \"{file}\" to inventory");
            Data.Files.Add(file);
        }

        /// <summary>
        /// Adds a file path to the inventory. This addition will not be
        /// saved back to file unil <c>Inventory.Save()</c> is getting called.
        /// </summary>
        /// <param name="regKey">The full path of the registry key to add.</param>
        public void AddRegistryKey(string regKey)
        {
            Log.Information($"Adding registry key \"{regKey}\" to inventory");
            Data.RegistryKeys.Add(regKey);
        }
    }

    /// <summary>
    /// The data model for our installation inventory.
    /// </summary>
    public class InventoryModel
    {
        /// <summary>
        /// A list of directory paths created by the installer.
        /// </summary>
        public HashSet<string> Directories { get; set; } = new HashSet<string>();

        /// <summary>
        /// A list of file paths created by the installer.
        /// </summary>
        public HashSet<string> Files { get; set; } = new HashSet<string>();

        /// <summary>
        /// A list of registry keys created by the installer.
        /// </summary>
        public HashSet<string> RegistryKeys { get; set; } = new HashSet<string>();
    }
}
