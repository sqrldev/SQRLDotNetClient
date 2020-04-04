using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace GitHubApi
{
    /// <summary>
    /// A helper class to check for new releases on Github.
    /// </summary>
    public static class GitHubHelper
    {
        public static readonly string SQRLInstallerUserAgent="Open Source Cross Platform SQRL Installer";
        public static readonly string SQRLVersionFile = "sqrlversion.json";
        public static readonly string SQRLGithubProjectOwner = "sqrldev";
        public static readonly string SQRLGithubProjectName = "SQRLDotNetClient";

        /// <summary>
        /// Retrieves information about releases from Github.
        /// </summary>
        public async static Task<GithubRelease[]> GetReleases()
        {
            var releases = await Task.Run(() =>
            {
                string jsonData = "";
                var wc = new WebClient
                {
                    CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
                };

                wc.Headers.Add("User-Agent", SQRLInstallerUserAgent);
                try
                {
                    jsonData = wc.DownloadString($"https://api.github.com/repos/{SQRLGithubProjectOwner}/{SQRLGithubProjectName}/releases");
                }
                catch (Exception ex)
                {
                    Log.Error($"Error Downloading Releases. Error: {ex}");
                }
                return !string.IsNullOrEmpty(jsonData) ? 
                    (Newtonsoft.Json.JsonConvert.DeserializeObject<List<GithubRelease>>(jsonData)).ToArray() :
                    new GithubRelease[] { };
            });
            return releases;
        }

        /// <summary>
        /// Downloads the resource specified by <paramref name="url"/> to the local file path
        /// specified by <paramref name="target"/>.
        /// </summary>
        /// <param name="url">The remote resource to download.</param>
        /// <param name="target">The full local file path for the download.</param>
        /// <returns>Returns <c>true</c> on success or <c>false</c> if an exception occures.</returns>
        public static bool DownloadFile(string url, string target)
        {
            bool success = true;
            var wc = new WebClient
            {
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
            };

            wc.Headers.Add("User-Agent", SQRLInstallerUserAgent);
            try
            {
                wc.DownloadFile(url,target);
            }
            catch (Exception ex)
            {
                Log.Error($"Error Downloading: {url} Error: {ex}");
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Checks if a newer version of the app exists in the project's Github releases.
        /// </summary>
        /// <returns>Returns <c>true</c> if an update exists, or <c>false</c> if no update exists
        /// or if the update check could not be perfomred for some reason.</returns>
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
