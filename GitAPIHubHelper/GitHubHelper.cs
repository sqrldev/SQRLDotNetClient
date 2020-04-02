using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;

namespace GitAPIHubHelper
{
    public static class GitHubHelper
    {
        public static GithubRelease[] GetReleases()
        {
            string jsonData = "";
            var wc =new WebClient
            {
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
            };

            wc.Headers.Add("User-Agent", "SQRL-Intaller");
            try
            {
                jsonData = wc.DownloadString("https://api.github.com/repos/sqrldev/SQRLDotNetClient/releases");
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error Downloading Releases");
            }
            return (Newtonsoft.Json.JsonConvert.DeserializeObject<List<GithubRelease>>(jsonData)).ToArray();
        }

        public static bool DownloadFile(string url, string target)
        {
            bool success = true;
            var wc = new WebClient
            {
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
            };

            wc.Headers.Add("User-Agent", "SQRL-Intaller");
            try
            {
                wc.DownloadFile(url,target);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Downloading: {url}");
                success = false;
            }
            return success;
        }

        public static bool CheckForUpdates()
        {
            bool updateAvailable = false;
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (File.Exists(Path.Combine(directory, "sqrlversion.json")))
            {
                string tag = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(File.ReadAllText(Path.Combine(directory, "sqrlversion.json")));
                var releases = GitAPIHubHelper.GitHubHelper.GetReleases();
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
