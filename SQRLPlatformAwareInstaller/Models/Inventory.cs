using SQRLCommonUI.Models;
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
        /// Gets the singleton instance for this class.
        /// </summary>
        public static Inventory Instance
        {
            get
            {
                if (_instance == null) _instance = new Inventory();
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
            if (!File.Exists(InventoryFile))
            {
                this.Data = new InventoryModel();
                return;
            }

            var json = File.ReadAllText(InventoryFile);
            this.Data = JsonSerializer.Deserialize<InventoryModel>(json);
        }

        /// <summary>
        /// Saves the curent inventory state to the json file.
        /// </summary>
        public void Save()
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(this.Data, this.Data.GetType(), options);
            File.WriteAllText(InventoryFile, json);
        }
    }


    public class InventoryModel
    {
        public HashSet<string> Directories { get; set; } = new HashSet<string>();
        public HashSet<string> Files { get; set; } = new HashSet<string>();
        public HashSet<string> RegistryKeys { get; set; } = new HashSet<string>();
    }
}
