﻿using Microsoft.Win32;
using Serilog;
using SQRLCommon.Models;
using SQRLPlatformAwareInstaller.Models;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

namespace SQRLPlatformAwareInstaller.Platform.Windows
{
    class Installer : IInstaller
    {
        #pragma warning disable 1998
        public async Task Install(string archiveFilePath, string installPath, string versionTag)
        {
            // The "async" in the delegate is needed, otherwise exceptions within
            // the delegate won't "bubble up" to the exception handlers upstream.
            await Task.Run(async () =>
            {
                Inventory.Instance.Load();
                Log.Information($"Installing on Windows to {installPath}");

                // Checking for Visual C++redistributable runtime
                Log.Information($"Checking for Visual C++ redistributable runtime");
                if (!VcRedistHelper.IsRuntimeInstalled())
                {
                    Log.Warning("Visual C++ redistributable not found, installing");
                    var exitCode = VcRedistHelper.InstallRuntime();
                    Log.Information($"Installation of Visual C++ redistributable returned with exit code {exitCode}");
                }
                else
                    Log.Information("Visual C++ redistributable was found");

                // Extract installation archive
                Log.Information($"Extracting main installation archive");
                CommonUtils.ExtractZipFile(archiveFilePath, string.Empty, installPath);

                // Check if a database exists in the installation directory 
                // (which is bad) and if it does, move it to user space.
                if (File.Exists(Path.Combine(installPath, PathConf.DBNAME)))
                {
                    Utils.MoveDb(Path.Combine(installPath, PathConf.DBNAME));
                }

                Inventory.Instance.AddDirectory(installPath);

                // Create registry keys for sqrl:// scheme registration
                Log.Information("Creating registry keys for sqrl:// protocol scheme");
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl"))
                {
                    Inventory.Instance.AddRegistryKey(key.ToString());
                    key.SetValue(string.Empty, "URL:SQRL Protocol");
                    key.SetValue("URL Protocol", $"", RegistryValueKind.String);
                }
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl\DefaultIcon"))
                {
                    key.SetValue("", $"{(GetClientExePath(installPath))},0", RegistryValueKind.String);
                }
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl\shell\open\command"))
                {
                    key.SetValue("", $"\"{(GetClientExePath(installPath))}\" \"%1\"", RegistryValueKind.String);
                }

                // Create uninstall registry entries
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SQRL OSS Client"))
                {
                    Inventory.Instance.AddRegistryKey(key.ToString());
                    key.SetValue("DisplayName", "SQRL Open Source Client");
                    key.SetValue("DisplayVersion", versionTag);
                    key.SetValue("DisplayIcon", $"{GetClientExePath(installPath)},0");
                    key.SetValue("UninstallString", $"\"{GetInstallerExePath(installPath)}\" -uninstall");
                    key.SetValue("Publisher", "SQRL Developers");
                    key.SetValue("URLInfoAbout", "https://github.com/sqrldev/SQRLDotNetClient");
                    key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                    key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    key.SetValue("InstallLocation", installPath);
                }

                // Create Desktop Shortcut
                Log.Information("Create Windows desktop shortcut");
                string shortcutLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SQRL OSS Client.lnk");
                IShellLink link = (IShellLink)new ShellLink();
                link.SetDescription("SQRL OSS Client");
                link.SetPath(GetClientExePath(installPath));
                link.SetIconLocation(GetClientExePath(installPath), 0);
                link.SetWorkingDirectory(installPath);
                IPersistFile iconFile = (IPersistFile)link;
                iconFile.Save(shortcutLocation, false);
                Inventory.Instance.AddFile(shortcutLocation);

                Inventory.Instance.Save();
            });
        }
        #pragma warning restore 1998

        public async Task Uninstall(IProgress<Tuple<int, string>> progress = null, bool dryRun = true)
        {
            await Uninstaller.Run(progress, dryRun);
        }

        public string GetClientExePath(string installPath)
        {
            return Path.Combine(installPath, "SQRLDotNetClientUI.exe");
        }

        public string GetInstallerExePath(string installPath)
        {
            return Path.Combine(installPath, "SQRLPlatformAwareInstaller_win.exe");
        }
    }
}
