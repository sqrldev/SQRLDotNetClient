using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitHubApi
{
    /// <summary>
    /// A helper class to check for new releases on Github.
    /// </summary>
    public static class GitHubHelper
    {
        public static readonly string SQRLInstallerUserAgent="Open Source Cross Platform SQRL Installer";
        public static readonly string SQRLGithubProjectOwner = "sqrldev";
        public static readonly string SQRLGithubProjectName = "SQRLDotNetClient";

        /// <summary>
        /// Retrieves information about releases from Github.
        /// </summary>
        public async static Task<GithubRelease[]> GetReleases()
        {
            return await Task.Run(() =>
            {
                string jsonData = "";
                var wc = new WebClient
                {
                    CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
                };

                AddHeaders(wc);

                try
                {
                    jsonData = wc.DownloadString($"https://api.github.com/repos/{SQRLGithubProjectOwner}/{SQRLGithubProjectName}/releases");
                }
                catch (Exception ex)
                {
                    Log.Error($"Error Downloading Releases. Error: {ex}");
                }
                return !string.IsNullOrEmpty(jsonData) ? 
                    (JsonConvert.DeserializeObject<List<GithubRelease>>(jsonData)).ToArray() :
                    new GithubRelease[] { };
            });
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
            var wc = new WebClient
            {
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
            };

            AddHeaders(wc);

            try
            {
                wc.DownloadFile(url,target);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error Downloading: {url} Error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a newer version of the app exists in the project's Github releases.
        /// </summary>
        /// <returns>Returns <c>true</c> if an update exists, or <c>false</c> if no update exists
        /// or if the update check could not be perfomred for some reason.</returns>
        public async static Task<bool> CheckForUpdates(Version currentVersion)
        {
            var releases = await GetReleases();
            if (releases != null && releases.Length > 0)
            {
                Match match = Regex.Match(releases[0].tag_name, @"\d+(?:\.\d+)+");
                if (match.Success)
                {
                    Version releaseVersion;
                    bool success = Version.TryParse(match.Value, out releaseVersion);

                    if (success && releaseVersion > currentVersion)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Adds the "User-Agent" header to the given <paramref name="webClient"/>.
        /// If a file with a name of "GithubAuthToken.txt" exists in the same directory
        /// as the executable, and the file contains a valid Github authorization token
        /// in the format of "token 5c19b5ada557335de35ed54642c363f82ac1da18", a 
        /// corresponding "Authorization" header will be sent as well.
        /// </summary>
        /// <param name="webClient">The WebClient to which headers should be added.</param>
        private static void AddHeaders(WebClient webClient)
        {
            webClient.Headers.Add("User-Agent", SQRLInstallerUserAgent);

            var authFile = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                @"GithubAuthToken.txt");

            if (File.Exists(authFile))
            {
                string authToken = File.ReadAllText(authFile);

                if (!string.IsNullOrEmpty(authToken))
                {
                    webClient.Headers.Add("Authorization", authToken);
                }
            }
        }
    }
}
