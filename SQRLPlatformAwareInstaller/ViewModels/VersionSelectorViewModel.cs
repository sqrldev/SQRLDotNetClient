using ReactiveUI;
using SQRLPlatformAwareInstaller.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SQRLPlatformAwareInstaller.ViewModels
{
    public class VersionSelectorViewModel : ViewModelBase
    {

        private string platform;
        private WebClient wc;
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
            this.wc = new WebClient();
            this.wc.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            this.wc.Headers.Add("User-Agent", "SQRL-Intaller");
            this.Releases = (Newtonsoft.Json.JsonConvert.DeserializeObject<GithubReleases>(wc.DownloadString("https://api.github.com/repos/sqrldev/SQRLDotNetClient/releases"))).Releases;
            this.platform = platform;
        }

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
            set { this.RaiseAndSetIfChanged(ref _selectedRelease, value); }
        }
    }
}
