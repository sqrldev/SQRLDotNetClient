using Serilog;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using ToolBox.Bridge;

namespace SQRLCommon.Models
{
    /// <summary>
    /// A helper class for reading and writing the config file
    /// that specifies app installation and database paths.
    /// </summary>
    public static class PathConf
    {
        private static IBridgeSystem _bridgeSystem { get; set; } = BridgeSystem.Bash;
        private static ShellConfigurator _shell { get; set; } = new ShellConfigurator(_bridgeSystem);
        private static PathConfModel _model = new PathConfModel();


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
                string path = "";
                
                // If this is running on linux we and running as admin we need to hack our way
                // to find the current user's home directory so that we get the correct config path.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && SystemAndShellUtils.IsAdmin())
                {
                    path = Path.Combine(SystemAndShellUtils.GetHomePath(), ".config", "SQRL", "sqrl.conf");
                }
                else
                {
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
                        Environment.SpecialFolderOption.Create), "SQRL", "sqrl.conf");
                }
                    
                return path;
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

                // If this is running on linux we and running as admin we need to hack our way
                // to find the current user's home directory so that we get the correct Default DB path
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && SystemAndShellUtils.IsAdmin())
                {
                    defaultDbPath = Path.Combine(SystemAndShellUtils.GetHomePath(), "SQRL");
                }
                else
                {
                    defaultDbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SQRL");
                }

                return defaultDbPath;
            }
        }

        /// <summary>
        /// The default client installation directory.
        /// </summary>
        public static readonly string DefaultLogPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
            Path.Combine("/var/log", "SQRL") :
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SQRL", "Logs");

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
        /// Gets or sets the SQRL client's database directory path.
        /// </summary>
        public static string LogPath
        {
            get
            {
                LoadConfig();
                return _model.LogPath;
            }
            set
            {
                _model.LogPath = value;
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
            // In no config file exists, "loading" shall reset the config to 
            // default values. We achieve this by simply creating a new model.
            _model = new PathConfModel();

            // If no config file exists, we're done
            if (!File.Exists(ConfFile)) return;

            // Try loading the values from file
            try
            {
                using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(ConfFile)))
                {
                    JsonElement value;
                    if (document.RootElement.TryGetProperty(nameof(_model.ClientInstallPath), out value))
                    {
                        _model.ClientInstallPath = value.GetString();
                    }
                    if (document.RootElement.TryGetProperty(nameof(_model.ClientDBPath), out value))
                    {
                        _model.ClientDBPath = value.GetString();
                    }
                    if (document.RootElement.TryGetProperty(nameof(_model.LogPath), out value))
                    {
                        _model.LogPath = value.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error deserializing path config file:\r\n{ex}");
            }
            finally
            {
                if (_model == null)
                    _model = new PathConfModel();

                // Sanitize loaded values  
                if (string.IsNullOrEmpty(_model.ClientInstallPath)) _model.ClientInstallPath = DefaultClientInstallPath;
                if (string.IsNullOrEmpty(_model.ClientDBPath)) _model.ClientDBPath = DefaultClientDBPath;
                if (string.IsNullOrEmpty(_model.LogPath)) _model.LogPath = DefaultLogPath;

                // Since loading may have sanitized or amended new values, we
                // save the config here to that changes are written back to file.
                SaveConfig();
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

            // Because in Linux "Root Owns Everything", we need to change the owner of the 
            // config back to our current user
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && SystemAndShellUtils.IsAdmin())
            {
                var user = SystemAndShellUtils.GetCurrentUser();
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

        /// <summary>
        /// The directory where the SQRL client and installer store their log files.
        /// </summary>
        public string LogPath { get; set; } = PathConf.DefaultLogPath;
    }
}
