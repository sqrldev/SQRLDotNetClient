using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace SQRLPlatformAwareInstaller.Models
{
    /// <summary>
    /// This enum is used by the installer to determine the action that was requested to be performed.
    /// By default, it would be "Install", but we also have the option to "Uninstall" and eventually 
    /// "Update" the application.
    /// </summary>
    public enum InstallerAction
    {
        Install,
        Uninstall,
        Update
    }

    /// <summary>
    /// This class holds all our allowed command line switches.
    /// </summary>
    public class CommandLineArgs
    {
        /// <summary>
        /// Command line switch -a | -action. <see cref="InstallerAction"/>
        /// </summary>
        [Option('a', "action", Required = false, Default = InstallerAction.Install, 
            HelpText = "Specifies the action to be performed (Install, Uninstall, Update).")]
        public InstallerAction Action { get; set; }

        /// <summary>
        /// Command line switch -p | -path "path". Specifies SQRL's current install path (directory).
        /// </summary>
        [Option('p', "path", Required = false,  Default = "", 
            HelpText = "Specifies SQRL's current Install path (directory)")]
        public string InstallPath { get; set; }

        /// <summary>
        /// Command line switch -z | zip "path". Specifies the location of the temporary 
        /// update zip file (used in conjunction with update action). If this is set, the
        /// "version" argument has to be provided, too.
        /// </summary>
        [Option('z', "zip", Required = false, Default = "", 
            HelpText = "Specifies the location of the temporary update zip file (used in conjunction with update action)")]
        public string ZipFilePath { get; set; }

        /// <summary>
        /// Command line switch -v | version "version tag". When used in conjunction with the 
        /// "install" action, specifies the version tag of the release to pre-select in the UI.
        /// When used in conjunction with the "update" action, specifies the version tag of the
        /// update archive provided with the <see cref="ZipFilePath">-z</see> switch.
        /// </summary>
        [Option('v', "version", Required = false, Default = "",
            HelpText = "Specifies the version tag to preselect (install) or the version tag of the existing update zip file (update)")]
        public string VersionTag { get; set; }

        /// <summary>
        /// Provides static access to the command line arguments passed to the
        /// currently running application.
        /// </summary>
        public static CommandLineArgs Instance
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                var r = Parser.Default.ParseArguments<CommandLineArgs>(args);
                if (r.Tag == ParserResultType.Parsed)
                {
                    return ((Parsed<CommandLineArgs>)r).Value;
                }
                else
                {
                    return new CommandLineArgs()
                    {
                        Action = InstallerAction.Install,
                        InstallPath = null,
                        ZipFilePath = null,
                        VersionTag = null
                    };
                }
            }
        }

        public override string ToString()
        {
            string a = string.IsNullOrEmpty(this.Action.ToString()) ? "" : $"-a {this.Action} ";
            string p = string.IsNullOrEmpty(this.InstallPath.ToString()) ? "" : $"-p {this.InstallPath} ";
            string z = string.IsNullOrEmpty(this.ZipFilePath.ToString()) ? "" : $"-z {this.ZipFilePath} ";
            string v = string.IsNullOrEmpty(this.VersionTag.ToString()) ? "" : $"-a {this.VersionTag} ";

            return $"{a}{p}{z}{v}";
        }
    }
}
