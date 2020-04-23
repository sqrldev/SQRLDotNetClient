using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// <summary>
        /// The HTTP user agent for the installer.
        /// </summary>
        public static readonly string SQRLInstallerUserAgent="Open Source Cross Platform SQRL Installer";

        /// <summary>
        /// The Github name of the project owner. (The middle part of a repository's github url.)
        /// </summary>
        public static readonly string SQRLGithubProjectOwner = "sqrldev";

        /// <summary>
        /// The Github project/repository name. (The last part of a repository's github url.)
        /// </summary>
        public static readonly string SQRLGithubProjectName = "SQRLDotNetClient";

        /// <summary>
        /// Specifies the keyword that, when present in a release tag, tells
        /// the release checker to omit those releases if we're not in 
        /// testing mode (<seealso cref="_testReleaseFile"/>).
        /// </summary>
        private static readonly string _testReleaseKeyword = "test";

        /// <summary>
        /// Specifies the "magic" file name for switching to testing/dev mode.
        /// If a file with this name is present in the executable's directory,
        /// test releases will be included in the release list (<seealso cref="_testReleaseKeyword"/>.
        /// </summary>
        private static readonly string _testReleaseFile = "TESTING";

        /// <summary>
        /// Defines the file name for the Github authrorization file which must
        /// contain a valid Github authorization token in the format of  
        /// "token 5c19b5ada557335de35ed54642c363f82ac1da18" and be placed in the
        /// executable's working directory. This is for development only!
        /// </summary>
        private static readonly string _githubAuthFile = "GithubAuthToken.txt";

        /// <summary>
        /// Retrieves information about releases from Github.
        /// </summary>
        public async static Task<GithubRelease[]> GetReleases(bool enablePreReleases = false)
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
                    Log.Error($"Error downloading releases: {ex}");
                }
                var releases = !string.IsNullOrEmpty(jsonData) ? 
                    (JsonConvert.DeserializeObject<List<GithubRelease>>(jsonData)).ToArray() :
                    new GithubRelease[] { };

                var testFile = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                _testReleaseFile);

                // If a file with the specified "magic" name exists in the executable's
                // directory, we show all releases, otherwise we hide those which contain 
                // the defined test keyword.
                releases = File.Exists(testFile) ? 
                    releases : 
                    releases.Where(x => !x.tag_name.ToLower().Contains(_testReleaseKeyword)).ToArray();

                /// Do we want pre-releases included?
                releases = enablePreReleases ?
                    releases :
                    releases.Where(x => !x.prerelease).ToArray();

                return releases;
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
        /// If a file with the name defined in <see cref="_githubAuthFile"/> exists in the 
        /// same directory as the executable, and the file contains a valid Github authorization 
        /// token in the format of "token 5c19b5ada557335de35ed54642c363f82ac1da18", a 
        /// corresponding "Authorization" header will be sent as well.
        /// </summary>
        /// <param name="webClient">The WebClient to which headers should be added.</param>
        private static void AddHeaders(WebClient webClient)
        {
            webClient.Headers.Add("User-Agent", SQRLInstallerUserAgent);

            var authFile = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                _githubAuthFile);

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
