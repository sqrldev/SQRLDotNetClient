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

namespace SQRLCommonUI.Models
{
    /// <summary>
    /// A helper class to check for new releases on Github.
    /// </summary>
    public static class GithubHelper
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
        /// testing mode (<seealso cref="_testEnvironmentVariable"/>).
        /// </summary>
        private static readonly string _testReleaseKeyword = "test";

        /// <summary>
        /// Specifies the environment variable name for switching to testing/dev mode.
        /// If an environment variable with this name is present and non-empty in the system,
        /// test releases will be included in the release list (<seealso cref="_testReleaseKeyword"/>.
        /// </summary>
        private static readonly string _testEnvironmentVariable = "SQRL_TESTMODE";

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
        /// <param name="enablePreReleases">If set to <c>true</c>, pre-releases will be
        /// included in the release listing.</param>
        /// <param name="fromFile">If set to <c>true</c>, the releases will not be fetched
        /// from Github, but instead be read from a local file.</param>
        public async static Task<GithubRelease[]> GetReleases(bool enablePreReleases = false, bool fromFile = false)
        {
            string source = fromFile ? "local file" : "Github";
            Log.Information($"Getting releases from {source}");

            return await Task.Run(() =>
            {
                string jsonData = "";

                try
                {
                    if (fromFile)
                    {
                        jsonData = File.ReadAllText(CommonUtils.GetReleasesFilePath());
                    }
                    else
                    {
                        var wc = new WebClient
                        {
                            CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
                        };
                        AddHeaders(wc);
                        jsonData = wc.DownloadString($"https://api.github.com/repos/{SQRLGithubProjectOwner}/{SQRLGithubProjectName}/releases");
                        File.WriteAllText(CommonUtils.GetReleasesFilePath(), jsonData);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error downloading or reading releases:\r\n{ex}");
                }

                var releases = !string.IsNullOrEmpty(jsonData) ? 
                    (JsonConvert.DeserializeObject<List<GithubRelease>>(jsonData)).ToArray() :
                    new GithubRelease[] { };

                if (fromFile) return releases;

                // If the test mode environment variable exists,
                // we show all releases, otherwise we hide test releases.
                var envVar = Environment.GetEnvironmentVariable(_testEnvironmentVariable);
                if (!string.IsNullOrEmpty(envVar))
                {
                    Log.Information("Testing environment variable found, enabling test releases!");
                }
                else
                {
                    // Remove test releases
                    releases = releases.Where(x => !x.tag_name.ToLower()
                    .Contains(_testReleaseKeyword)).ToArray();
                }

                // Do we want pre-releases included?
                if (!enablePreReleases)
                {
                    // Remove pre-releases
                    releases = releases.Where(x => !x.prerelease).ToArray();
                }

                return releases;
            });
        }

        /// <summary>
        /// Downloads the zip archive of the latest release to a local file and 
        /// returns the full path to that file.
        /// </summary>
        /// <param name="enablePreReleases">If set to <c>true</c>, pre-releases will be
        /// included when determining the latest release.</param>
        /// <returns>Returns the full file path of the downloaded file.</returns>
        public async static Task<string> DownloadLatestRelease(bool enablePreReleases = false)
        {
            return await Task.Run(async () =>
            {
                var releases = await GetReleases(enablePreReleases);
                if (releases.Length < 1)
                {
                    throw new Exception("No releases found!");
                }
                var latestRelease = releases.OrderBy(x => x.created_at).First();
                var downloadLink = CommonUtils.GetDownloadLinkByPlatform(latestRelease);

                var fileName = Path.GetFileName(downloadLink);
                var tempFilePath = Path.GetTempFileName();

                if (!DownloadFile(downloadLink, tempFilePath))
                {
                    throw new Exception("Error downloading release!");
                }

                return tempFilePath;
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
        /// /// <param name="enablePreReleases">If set to <c>true</c>, pre-releases will be
        /// included in the release listing.</param>
        public async static Task<bool> CheckForUpdates(Version currentVersion, bool enablePreReleases = true)
        {
            var releases = await GetReleases(enablePreReleases);
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
