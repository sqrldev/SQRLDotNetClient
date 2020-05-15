using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace SQRLPlatformAwareInstaller.Models
{
    public enum InstallerAction
    {
        Install,
        Uninstall,
        Update
    }
    public class CommandLineArgs
    {
        [Option('a',"action",Required =false, Default = InstallerAction.Install, HelpText = "Specifies the action to be performed (Install, Uninstall, Update).")]
        public InstallerAction Action { get; set; }

        [Option('p', "path", Required = false,  Default ="", HelpText = "Specifies SQRL's current Install path (directory)")]
        public string InstallPath { get; set; }

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
