﻿using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using ReactiveUI;
using Serilog;
using SQRLCommon.Models;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    /// <summary>
    /// A view model representing the main installer screen.
    /// </summary>
    public class MainInstallViewModel: ViewModelBase
    {
        private Bitmap _platformImg;
        private int _testingModeCounter = 0;
        
        /// <summary>
        /// An image representing the current OS platform.
        /// </summary>
        public Bitmap PlatformImg
        {
            get
            {
                return _platformImg;
            }
            set { this.RaiseAndSetIfChanged(ref _platformImg, value); }
        }

        /// <summary>
        /// A string representing the current OS platform.
        /// </summary>
        public string Platform
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "Windows";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "MacOSX";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "Linux";
                else
                    return "";
            }
        }

        /// <summary>
        /// Gets a string representing the installer version.
        /// </summary>
        public string Version
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName()
                    .Version.ToString();
            }
        }

        /// <summary>
        /// Creates a new instance, sets the window title and 
        /// starts the platform detection.
        /// </summary>
        public MainInstallViewModel()
        {
            Log.Information("Installer main screen launched");

            this.Title = _loc.GetLocalizationValue("TitleMainInstall");
            DetectPlatform();
            Log.Information($"Detected platform: {Platform}");
        }

        /// <summary>
        /// Detects the OS platform and sets some UI elements accordingly.
        /// </summary>
        public void DetectPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                this.PlatformImg = new Bitmap(_assets.Open(new Uri("resm:SQRLPlatformAwareInstaller.Assets.windows.png")));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                this.PlatformImg = new Bitmap(_assets.Open(new Uri("resm:SQRLPlatformAwareInstaller.Assets.mac.png")));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                this.PlatformImg = new Bitmap(_assets.Open(new Uri("resm:SQRLPlatformAwareInstaller.Assets.linux.png")));
            else
                this.PlatformImg = new Bitmap(_assets.Open(new Uri("resm:SQRLPlatformAwareInstaller.Assets.unknown.png")));
        }

        /// <summary>
        /// Enables testing mode when clicking on the version label 5 times.
        /// </summary>
        public void EnableTestingMode()
        {
            _testingModeCounter++;
            if (_testingModeCounter == 5)
            {
                Environment.SetEnvironmentVariable(GithubHelper.TestModeEnvVar, "true");
                Log.Information("Testing mode enabled");
            }
        }

        /// <summary>
        /// Moves on to the version selection screen.
        /// </summary>
        public void Next()
        {
            ((MainWindowViewModel)_mainWindow.DataContext).Content = 
                new VersionSelectorViewModel();
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        public void Cancel()
        {
            Environment.Exit(0);
        }
    }
}
