using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace SQRLPlatformAwareInstaller.Models
{
    /// <summary>
    /// This enum is used by the installer to determine the action that was requested to be performed
    /// by default it would be Install, but we also have the option to Unininstall and eventually upgrade the application.
    /// </summary>
    public enum InstallerAction
    {
        Install,
        Uninstall,
        Update
    }

    /// <summary>
    /// This class holds all our allowed command line switches
    /// </summary>
    public class CommandLineArgs
    {
        /// <summary>
        /// Command line switch -a | -action <see cref="InstallerAction"/>
        /// </summary>
        [Option('a',"action",Required =false, Default = InstallerAction.Install, HelpText = "Specifies the action to be performed (Install, Uninstall, Update).")]
        public InstallerAction Action { get; set; }

        /// <summary>
        /// Command line switch -p | -path "path hold's SQRL current install path
        /// </summary>
        [Option('p', "path", Required = false,  Default ="", HelpText = "Specifies SQRL's current Install path (directory)")]
        public string InstallPath { get; set; }

        /// <summary>
        /// Command line switch -z | zip "path to update zip" holds the path ofthe downloaded update zip
        /// </summary>
        [Option('z', "zip", Required = false, Default = "", HelpText = "Specifies the location of the temporary update zip file (used in conjunction with update action)")]
        public string TempAzipPath { get; set; }

        public override string ToString()
        {
            return $"-a {this.Action} -p: {this.InstallPath} -z: {this.TempAzipPath}";
        }
    }

    public static class InstallerCommands
    {
        public static SQRLPlatformAwareInstaller.Models.CommandLineArgs Instance {
            get{
                string[] args = Environment.GetCommandLineArgs();
                var r = Parser.Default.ParseArguments<CommandLineArgs>(args);
                if (r.Tag == ParserResultType.Parsed)
                    return ((CommandLine.Parsed<SQRLPlatformAwareInstaller.Models.CommandLineArgs>)r).Value;
                else
                    return null;  
            } }
    }
}
