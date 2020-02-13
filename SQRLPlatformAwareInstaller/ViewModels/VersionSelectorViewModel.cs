using ReactiveUI;
using SQRLPlatformAwareInstaller.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Linq;
using System.IO;
using Avalonia.Controls;
using SQRLPlatformAwareInstaller.Views;
using Avalonia;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    public class VersionSelectorViewModel : ViewModelBase
    {

        private string platform;
        private WebClient wc;
        private string Executable = "";

        private int _DownloadPercentage;
        public int DownloadPercentage { get { return _DownloadPercentage; } set { this.RaiseAndSetIfChanged(ref _DownloadPercentage, value); } }

        private string DownloadUrl = "";

        private string _InstallationPath;
        public string InstallationPath { get { return this._InstallationPath; } set { this.RaiseAndSetIfChanged(ref this._InstallationPath, value); } }
        public VersionSelectorViewModel(string platform)
        {
            InitObj(platform);
        }
        public VersionSelectorViewModel()
        {
            InitObj();
        }

        private void InitObj(string platform ="WINDOWS")
        {
            this.Title = "SQRL Client Installer - Version Selector";
            this.wc = new WebClient
            {
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
            };
            
            this.wc.Headers.Add("User-Agent", "SQRL-Intaller");
            this.Releases = (Newtonsoft.Json.JsonConvert.DeserializeObject<List<GithubRelease>>(wc.DownloadString("https://api.github.com/repos/sqrldev/SQRLDotNetClient/releases"))).ToArray();
            this.SelectedRelease = this.Releases.OrderByDescending(r => r.published_at).FirstOrDefault();
            this.platform = platform;
            PathByPlatForm(this.platform);
        }

        private void PathByPlatForm(string platform)
        {
            switch (this.platform)
            {

                case "MacOSX":
                case "Linux":
                    {
                        this.InstallationPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.None), "SQRL");
                    }
                    break;
                case "WINDOWS":
                default:
                    {
                        this.InstallationPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SQRL");
                    }
                    break;
            }
        }

        private string _installStatus;
        public string InstallStatus { get { return this._installStatus; }
        set
            {
                { this.RaiseAndSetIfChanged(ref _installStatus, value); }
            }
        }

        private string DownloadedFileName;

        private GithubRelease[] _release;
        public GithubRelease[] Releases
        {
            get { return this._release; }
            set { this.RaiseAndSetIfChanged(ref _release, value); }
        }

        private GithubRelease _selectedRelease;
        public GithubRelease SelectedRelease
        {
            get { return this._selectedRelease; }
            set { this.RaiseAndSetIfChanged(ref _selectedRelease, value); SetDownloadSize(); }
        }
        decimal? downloadSize;
        public decimal? DownloadSize
        {
            get
            {
                return this.downloadSize;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref downloadSize, value);
            }
            
        }

        public void SetDownloadSize()
        {
            switch (this.platform)
            {

                case "MacOSX":
                    {
                        this.DownloadSize = Math.Round((this.SelectedRelease.assets.Where(x => x.name.Contains("linux")).First().size / 1024M) / 1024M,2);
                        this.DownloadUrl = this.SelectedRelease.assets.Where(x => x.name.Contains("linux")).First().browser_download_url;
                    }
                    break;
                case "Linux":
                    {
                        this.DownloadSize=Math.Round((this.SelectedRelease.assets.Where(x => x.name.Contains("osx")).First().size / 1024M ) / 1024M,2);
                        this.DownloadUrl = this.SelectedRelease.assets.Where(x => x.name.Contains("osx")).First().browser_download_url;
                    }
                    break;
                case "WINDOWS":
                default:
                    {
                        this.DownloadSize=Math.Round((this.SelectedRelease.assets.Where(x => x.name.Contains("win64")).First().size / 1024M) / 1024M, 2);
                        this.DownloadUrl = this.SelectedRelease.assets.Where(x => x.name.Contains("win64")).First().browser_download_url;
                    }
                    break;
            }
        }

        public void DownloadInstall()
        {
            this.InstallStatus = "Downloading...";
            wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
            wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
            this.DownloadedFileName = Path.GetTempFileName();
            wc.Headers.Remove("Authorization");
            wc.DownloadFileAsync(new Uri(this.DownloadUrl), DownloadedFileName);
        }

        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            this.DownloadPercentage = 100;
            InstallOnPlatform(this.DownloadedFileName);
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = new InstallationCompleteViewModel(Path.Combine(this.Executable));
        }

        private void InstallOnPlatform(string downloadedFileName)
        {
            switch (this.platform)
            {
                case "MacOSX":
                    {
                        ;
                    }
                    break;
                case "Linux":
                    {
                        ;
                    }
                    break;
                case "WINDOWS":
                default:
                    {
                        InstallinWindodows(downloadedFileName);
                    }
                    break;
            }
        }

        private async void InstallinWindodows(string downloadedFileName)
        {
            this.InstallStatus = "Installing...";
            Executable = Path.Combine(this.InstallationPath, "SQRLDotNetClient.exe");
            await Task.Run(() =>
            {
                if (!Directory.Exists(this.InstallationPath))
                {
                    Directory.CreateDirectory(this.InstallationPath);
                }
                this.DownloadPercentage = 20;
                File.Move(downloadedFileName, Executable, true);
                this.DownloadPercentage += 20;
            });
            await Task.Run(() =>
            {
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl"))
                {
                    key.SetValue(string.Empty, "URL:SQRL Protocol");
                    key.SetValue("URL Protocol", $"", RegistryValueKind.String);
                    this.DownloadPercentage += 20;
                }
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl\DefaultIcon"))
                {
                    key.SetValue("", $"{(Executable)},1", RegistryValueKind.String);
                    this.DownloadPercentage += 20;
                }
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"sqrl\shell\open\command"))
                {
                    key.SetValue("", $"\"{(Executable)}\" \"%1\"", RegistryValueKind.String);
                    this.DownloadPercentage += 20;
                }
            });
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.DownloadPercentage = e.ProgressPercentage;
            this.InstallStatus = $"Downloading...{Math.Round(e.BytesReceived/1024M/1024M,2)}/{Math.Round(e.TotalBytesToReceive/1024M / 1024M,2)} MBs";
        }

        public async void FolderPicker()
        {
            OpenFolderDialog ofd = new OpenFolderDialog
            {
                Directory = Path.GetDirectoryName(this.InstallationPath),
                Title = "Select the Destination Directory for the SQRL Client Installation"
            };
            var result = await ofd.ShowAsync(AvaloniaLocator.Current.GetService<MainWindow>());
            if(!string.IsNullOrEmpty(result))
            {
                this.InstallationPath = Path.Combine(result, "SQRL");
            }
        }

        public void Cancel()
        {
            ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).PriorContent;
        }

    }
}
