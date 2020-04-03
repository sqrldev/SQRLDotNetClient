using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace GitHubApi
{
    public static class GitHubHelper
    {
        public static readonly string UserAgent="Open Source Cross Platform SQRL Installer";
        public static readonly string SQRLVersionFile = "sqrlversion.json";
        public async static Task<GithubRelease[]> GetReleases()
        {
            var releases = await Task.Run(() =>
            {
                string jsonData = "";
                var wc = new WebClient
                {
                    CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
                };

                wc.Headers.Add("User-Agent", UserAgent);
                try
                {
                    jsonData = wc.DownloadString("https://api.github.com/repos/sqrldev/SQRLDotNetClient/releases");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error Downloading Releases. Error: {ex}");
                }
                return (Newtonsoft.Json.JsonConvert.DeserializeObject<List<GithubRelease>>(jsonData)).ToArray();
            });
            return releases;
        }

        public static bool DownloadFile(string url, string target)
        {
            bool success = true;
            var wc = new WebClient
            {
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
            };

            wc.Headers.Add("User-Agent", UserAgent);
            try
            {
                wc.DownloadFile(url,target);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Downloading: {url} Error: {ex}");
                success = false;
            }
            return success;
        }

        public async static Task<bool> CheckForUpdates()
        {
            bool updateAvailable = false;
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (File.Exists(Path.Combine(directory, SQRLVersionFile)))
            {
                string tag = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(File.ReadAllText(Path.Combine(directory, SQRLVersionFile)));
                var releases = await GitHubApi.GitHubHelper.GetReleases();
                if (releases != null && !tag.Equals(releases[0].tag_name, StringComparison.OrdinalIgnoreCase))
                {
                    updateAvailable = true;
                }
                else
                    updateAvailable = false;
            }
            else
                updateAvailable = false;

            return updateAvailable;
        }
    }
}
