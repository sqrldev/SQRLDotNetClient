using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using ToolBox.Bridge;

namespace SQRLCommonUI.Models
{
    /// <summary>
    /// A helper class for reading and writing the config file
    /// that specifies app installation and database paths.
    /// </summary>
    public static class PathConf
    {
        private static PathConfModel _model = new PathConfModel();
        private static IBridgeSystem _bridgeSystem { get; set; } = BridgeSystem.Bash;
        private static ShellConfigurator _shell { get; set; } = new ShellConfigurator(_bridgeSystem);

        /// <summary>
        /// The file name, excluding the path, of the client database file.
        /// </summary>
        public static readonly string DBNAME = "sqrl.db";

        /// <summary>
        /// The full file path of the config file.
        /// </summary>
        public static string ConfFile
        {
            get
            {
                string cfPath = "";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && AdminCheck.IsAdmin())
                {
                    string user = _shell.Term("logname", Output.Hidden).stdout.Trim();
                    string home = _shell.Term($"getent passwd {user} | cut -d: -f6", Output.Hidden).stdout.Trim();
                    cfPath = Path.Combine(home, ".config", "SQRL", "sqrl.conf");
                }
                else
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "SQRL", "sqrl.conf");
                return cfPath;
            }
        }

        /// <summary>
        /// The default client installation directory.
        /// </summary>
        public static readonly string DefaultClientInstallPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
            Path.Combine("/opt", "SQRL") :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)) :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SQRL");

        /// <summary>
        /// The default client database directory.
        /// </summary>
        public static string DefaultClientDBPath
        {
            get
            {
                string defaultDbPath = "";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && AdminCheck.IsAdmin())
                {
                    string user = _shell.Term("logname", Output.Hidden).stdout.Trim();
                    string home = _shell.Term($"getent passwd {user} | cut -d: -f6", Output.Hidden).stdout.Trim();
                    defaultDbPath = Path.Combine(home, "SQRL");
                }
                else
                    defaultDbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SQRL");

                return defaultDbPath;
            }


        }

        /// <summary>
        /// Gets or sets the SQRL client installation directory path.
        /// </summary>
        public static string ClientInstallPath
        {
            get
            {
                LoadConfig();
                return _model.ClientInstallPath;
            }
            set
            {
                _model.ClientInstallPath = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// Gets or sets the SQRL client's database directory path.
        /// </summary>
        public static string ClientDBPath
        {
            get
            {
                LoadConfig();
                return _model.ClientDBPath;
            }
            set
            {
                _model.ClientDBPath = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// The full file path, including the file name, to the client database.
        /// </summary>
        public static string FullClientDbPath
        {
            get
            {
                LoadConfig();
                return Path.Combine(_model.ClientDBPath, PathConf.DBNAME);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the path config file exists or not.
        /// </summary>
        public static bool ConfigFileExists
        {
            get { return File.Exists(ConfFile); }
        }

        /// <summary>
        /// Reads all values from the config file. If the config file does not
        /// exist, all config properties will have their default value.
        /// </summary>
        public static void LoadConfig()
        {
            Log.Information($"Config File:{ConfFile}");
            if (!File.Exists(ConfFile))
            {
                
                // In no config file exists, "loading" shall reset the config to 
                // default values. We achieve this by simply creating a new model.
                _model = new PathConfModel();
            }
            else
            {
                try
                {
                    _model = JsonSerializer.Deserialize<PathConfModel>(File.ReadAllText(ConfFile));
                }
                catch (Exception ex)
                {
                    Log.Error($"Error deserializing path config file:\r\n{ex}");
                }
                finally
                {
                    if (_model == null)
                        _model = new PathConfModel();

                    if (string.IsNullOrEmpty(_model.ClientInstallPath))
                        _model.ClientInstallPath = DefaultClientInstallPath;

                    if (string.IsNullOrEmpty(_model.ClientDBPath))
                        _model.ClientDBPath = DefaultClientDBPath;
                }
            }
        }

        /// <summary>
        /// Writes all values to the config file. If the config file does not 
        /// exist, it gets created, along with all the directories involved.
        /// An existing file will get overwritten.
        /// </summary>
        public static void SaveConfig()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            if (_model == null) _model = new PathConfModel();
            Directory.CreateDirectory(Path.GetDirectoryName(ConfFile));
            string serialized = JsonSerializer.Serialize(_model, _model.GetType(), options);
            File.WriteAllText(ConfFile, serialized);

            // Because Root owns everything UGH!
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && AdminCheck.IsAdmin())
            {
                string user = _shell.Term("logname", Output.Hidden).stdout.Trim();
                _shell.Term($"chown {user} {ConfFile}");
                _shell.Term($"chown {user} {Path.GetDirectoryName(ConfFile)}");
            }
        }
    }

    /// <summary>
    /// A class representing config entries used for 
    /// serializing to / deserializing from a json file.
    /// </summary>
    public class PathConfModel
    {
        /// <summary>
        /// The installation directory of the SQRL client.
        /// </summary>
        public string ClientInstallPath { get; set; } = PathConf.DefaultClientInstallPath;

        /// <summary>
        /// The directory where the SQRL client's database is located.
        /// </summary>
        public string ClientDBPath { get; set; } = PathConf.DefaultClientDBPath;
    }
}
