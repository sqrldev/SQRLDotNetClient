using SQRLPlatformAwareInstaller.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQRLPlatformAwareInstaller.Platform.OSX
{
    class Installer : IInstaller
    {
        public void Install(string archiveFilePath, string installPath, IProgress<int> progress)
        {
            throw new NotImplementedException();
        }

        public void UnInstall(string uninstallInfoFile)
        {
            throw new NotImplementedException();
        }

        public string GetExecutablePath(string installPath)
        {
            return Path.Combine(installPath, "SQRL.app/Contents/MacOS", "SQRLDotNetClientUI");
        }
    }
}
