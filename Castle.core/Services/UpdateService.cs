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

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var release = await response.Content.ReadFromJsonAsync<GitHubRelease>();

            if (release == null || string.IsNullOrWhiteSpace(release.TagName))
            {
                return null;
            }

            return new UpdateInfo
            {
                CurrentVersion = AppVersion.Current,
                LatestVersion = release.TagName,
                ReleaseUrl = release.HtmlUrl,
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
        latest = latest.TrimStart('v', 'V');
        current = current.TrimStart('v', 'V');

        if (Version.TryParse(latest, out var latestVersion) &&
            Version.TryParse(current, out var currentVersion))
        {
            return latestVersion > currentVersion;
        }

        return !string.Equals(latest, current, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = "";
    }
}

public class UpdateInfo
{
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public string ReleaseUrl { get; set; } = "";
    public bool IsUpdateAvailable { get; set; }
}