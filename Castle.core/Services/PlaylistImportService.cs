using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Castle.Core.Services;

public class PlaylistImportService
{
    private readonly DownloadQueueService _downloadQueue;
    private readonly SearchService _searchService;
    private readonly PlaylistService _playlistService;

    public PlaylistImportService(DownloadQueueService downloadQueue, SearchService searchService, PlaylistService playlistService)
    {
        _downloadQueue = downloadQueue;
        _searchService = searchService;
        _playlistService = playlistService;
    }

    public async Task<(int count, string? playlistId)> ImportYouTubePlaylistAsync(string playlistUrl)
    {
        var ytdlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe");

        System.Diagnostics.Debug.WriteLine($"[YT] Starting import: {playlistUrl}");

        if (!File.Exists(ytdlpPath))
        {
            System.Diagnostics.Debug.WriteLine("[YT] yt-dlp.exe not found");
            return (0, null);
        }

        var videos = new List<(string artist, string title, string url)>();
        string playlistTitle = "Imported Playlist";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ytdlpPath,
                Arguments = $"--flat-playlist --print \"%(title)s||%(id)s||%(playlist_title)s||%(channel)s\" --no-warnings {playlistUrl}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                try
                {
                    var parts = e.Data.Split("||");
                    if (parts.Length >= 2)
                    {
                        var title = parts[0].Trim();
                        var id = parts[1].Trim();

                        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(id))
                        {
                            var url = $"https://youtube.com/watch?v={id}";

                            // Try to get artist from channel name first
                            var artist = "Unknown Artist";
                            if (parts.Length >= 4 && !string.IsNullOrWhiteSpace(parts[3]))
                                artist = parts[3].Trim();

                            var trackTitle = title;
                            var dashIndex = title.IndexOf(" - ");
                            if (dashIndex > 0)
                            {
                                artist = title[..dashIndex].Trim();
                                trackTitle = title[(dashIndex + 3)..].Trim();
                            }

                            if (videos.Count == 0 && parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2]))
                                playlistTitle = parts[2].Trim();

                            videos.Add((artist, trackTitle, url));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[YT] Parse error: {ex.Message}");
                }
            }
        };

        var errorOutput = new StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                errorOutput.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        if (videos.Count == 0)
        {
            var errMsg = errorOutput.ToString();
            System.Diagnostics.Debug.WriteLine($"[YT] No videos found. Error: {errMsg}");
            return (0, null);
        }

        System.Diagnostics.Debug.WriteLine($"[YT] Found {videos.Count} videos, playlist: {playlistTitle}");

        var playlist = _playlistService.CreatePlaylist(playlistTitle);
        string playlistId = playlist.Id;

        foreach (var (artist, trackTitle, url) in videos)
        {
            System.Diagnostics.Debug.WriteLine($"[YT] Adding to queue: {artist} - {trackTitle}");
            _downloadQueue.AddToQueue(url, trackTitle, artist, playlistId);
        }

        return (videos.Count, playlistId);
    }

#if SPOTIFY_SUPPORT
    public async Task<int> ImportSpotifyPlaylistAsync(string playlistUrl)
    {
        var playlistId = ExtractSpotifyPlaylistId(playlistUrl);
        
        if (string.IsNullOrEmpty(playlistId))
            return 0;

        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        http.DefaultRequestHeaders.Add("Accept", "application/json");
        
        var tracks = new List<(string title, string artist)>();
        
        try
        {
            var token = await GetSpotifyTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var apiUrl = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks?limit=100";
                var response = await http.GetStringAsync(apiUrl);
                var doc = JsonDocument.Parse(response);
                
                if (doc.RootElement.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("track", out var trackElement) && 
                            trackElement.ValueKind != JsonValueKind.Null)
                        {
                            var title = trackElement.TryGetProperty("name", out var t) ? t.GetString() ?? "" : "";
                            var artist = "Unknown Artist";
                            if (trackElement.TryGetProperty("artists", out var artists) && 
                                artists.GetArrayLength() > 0)
                            {
                                artist = artists[0].TryGetProperty("name", out var a) ? a.GetString() ?? "Unknown Artist" : "Unknown Artist";
                            }
                            
                            if (!string.IsNullOrWhiteSpace(title))
                                tracks.Add((title, artist));
                        }
                    }
                }
            }
        }
        catch { }
        
        int added = 0;
        foreach (var (title, artist) in tracks)
        {
            var results = await _searchService.SearchAsync($"{artist} - {title}", 1);
            if (results.Count > 0)
            {
                _downloadQueue.AddToQueue(
                    $"https://youtube.com/watch?v={results[0].VideoId}",
                    results[0].Title,
                    results[0].Author);
                added++;
            }
        }

        return tracks.Count;
    }

    private async Task<string?> GetSpotifyTokenAsync()
    {
        try
        {
            var http = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                var spotifyClientId = Preferences.Get("spotify_client_id", string.Empty);
var spotifyClientSecret = Preferences.Get("spotify_client_secret", string.Empty);

if (string.IsNullOrWhiteSpace(spotifyClientId) ||
    string.IsNullOrWhiteSpace(spotifyClientSecret))
{
    return null;
}

var content = new FormUrlEncodedContent(new[]
{
    new KeyValuePair<string, string>("grant_type", "client_credentials"),
    new KeyValuePair<string, string>("client_id", spotifyClientId),
    new KeyValuePair<string, string>("client_secret", spotifyClientSecret)
});
            });

            var response = await http.PostAsync("https://accounts.spotify.com/api/token", content);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.GetProperty("access_token").GetString();
        }
        catch { return null; }
    }
#endif

    private string? ExtractSpotifyPlaylistId(string url)
    {
        if (url.Contains("playlist/"))
        {
            var parts = url.Split("playlist/");
            if (parts.Length > 1)
                return parts[1].Split('?')[0];
        }
        else if (url.Contains("playlist:"))
        {
            var parts = url.Split("playlist:");
            if (parts.Length > 1)
                return parts[1].Split('?')[0];
        }

        return null;
    }
}