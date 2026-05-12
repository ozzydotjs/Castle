using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Castle.Services;

public class UpdateService
{
    private readonly HttpClient _httpClient = new();

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{AppVersion.GitHubOwner}/{AppVersion.GitHubRepo}/releases/latest";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("CastleDesktopApp");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var release = await response.Content.ReadFromJsonAsync<GitHubRelease>();

            if (release == null || string.IsNullOrWhiteSpace(release.TagName))
            {
                return null;
            }

            var installerAsset = release.Assets
                .FirstOrDefault(asset =>
                    asset.Name.Equals(AppVersion.InstallerAssetName, StringComparison.OrdinalIgnoreCase) ||
                    asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

            return new UpdateInfo
            {
                CurrentVersion = AppVersion.Current,
                LatestVersion = release.TagName,
                ReleaseName = release.ReleaseName,
                ReleaseNotes = release.Body,
                ReleaseUrl = release.HtmlUrl,
                InstallerDownloadUrl = installerAsset?.BrowserDownloadUrl ?? "",
                IsUpdateAvailable = IsNewerVersion(release.TagName, AppVersion.Current)
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        latest = NormalizeVersion(latest);
        current = NormalizeVersion(current);

        if (Version.TryParse(latest, out var latestVersion) &&
            Version.TryParse(current, out var currentVersion))
        {
            return latestVersion > currentVersion;
        }

        return !string.Equals(latest, current, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeVersion(string version)
    {
        return version
            .Trim()
            .TrimStart('v', 'V');
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("name")]
        public string ReleaseName { get; set; } = "";

        [JsonPropertyName("body")]
        public string Body { get; set; } = "";

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = "";

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = "";
    }
}

public class UpdateInfo
{
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public string ReleaseName { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public string ReleaseUrl { get; set; } = "";
    public string InstallerDownloadUrl { get; set; } = "";
    public bool IsUpdateAvailable { get; set; }
}