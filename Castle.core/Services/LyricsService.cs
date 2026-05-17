using System.Net.Http.Json;
using System.Text.Json;
using Castle.Core.Models;

namespace Castle.Core.Services;

public class LyricsService
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, List<LyricLine>> _cache = new();
    private readonly string _lyricsFolder;

    public LyricsService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        _http.DefaultRequestHeaders.Add("User-Agent", "Castle/1.0");

        _lyricsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Castle",
            "lyrics"
        );
        Directory.CreateDirectory(_lyricsFolder);
    }

    public async Task<List<LyricLine>?> GetLyricsAsync(string title, string artist)
    {
        var cacheKey = $"{title}|{artist}";

        // Check memory cache first
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached.Count == 0 ? null : cached;

        // Check local .lrc file
        var localLrc = LoadLocalLrc(title, artist);
        if (localLrc != null)
        {
            _cache[cacheKey] = localLrc;
            return localLrc;
        }

        // Fetch from API
        try
        {
            List<LyricLine>? result = await TryGetLyrics(title, artist)
                ?? await TryGetLyrics(title, "")
                ?? await SearchLyrics(title);

            _cache[cacheKey] = result ?? new List<LyricLine>();

            // Save to local .lrc file
            if (result != null && result.Count > 0)
            {
                SaveLocalLrc(title, artist, result);
            }

            return result;
        }
        catch
        {
            _cache[cacheKey] = new List<LyricLine>();
            return null;
        }
    }

    private string GetLrcFilePath(string title, string artist)
    {
        var safeName = SanitizeFileName($"{artist} - {title}");
        return Path.Combine(_lyricsFolder, $"{safeName}.lrc");
    }

    private List<LyricLine>? LoadLocalLrc(string title, string artist)
    {
        try
        {
            var filePath = GetLrcFilePath(title, artist);
            if (File.Exists(filePath))
            {
                var lrcContent = File.ReadAllText(filePath);
                var lyrics = ParseLrc(lrcContent);
                if (lyrics.Count > 0) return lyrics;
            }
        }
        catch { }
        return null;
    }

    private void SaveLocalLrc(string title, string artist, List<LyricLine> lyrics)
    {
        try
        {
            var filePath = GetLrcFilePath(title, artist);
            var lrc = LyricsToLrc(lyrics);
            File.WriteAllText(filePath, lrc);
        }
        catch { }
    }

    private string LyricsToLrc(List<LyricLine> lyrics)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var line in lyrics)
        {
            if (line.Timestamp > TimeSpan.Zero)
            {
                sb.AppendLine($"[{line.Timestamp.Minutes:D2}:{line.Timestamp.Seconds:D2}.{line.Timestamp.Milliseconds / 10:D2}]{line.Text}");
            }
            else
            {
                sb.AppendLine(line.Text);
            }
        }
        return sb.ToString();
    }

    private string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "unknown";
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        sanitized = sanitized.Trim('.', ' ');
        if (string.IsNullOrWhiteSpace(sanitized)) sanitized = "unknown";
        return sanitized.Length > 120 ? sanitized[..120].Trim() : sanitized;
    }

    private async Task<List<LyricLine>?> TryGetLyrics(string title, string artist)
    {
        var url = $"https://lrclib.net/api/get?track_name={Uri.EscapeDataString(title)}";
        if (!string.IsNullOrWhiteSpace(artist))
            url += $"&artist_name={Uri.EscapeDataString(artist)}";

        try
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("syncedLyrics", out var se) &&
                se.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(se.GetString()))
                return ParseLrc(se.GetString()!);

            if (doc.RootElement.TryGetProperty("plainLyrics", out var pe) &&
                pe.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(pe.GetString()))
                return CreatePlainLyrics(pe.GetString()!);
        }
        catch { }
        return null;
    }

    private async Task<List<LyricLine>?> SearchLyrics(string title)
    {
        var url = $"https://lrclib.net/api/search?q={Uri.EscapeDataString(title)}";
        try
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonDocument.Parse(json).RootElement;

            foreach (var result in results.EnumerateArray())
            {
                if (result.TryGetProperty("syncedLyrics", out var se) &&
                    se.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(se.GetString()))
                    return ParseLrc(se.GetString()!);

                if (result.TryGetProperty("plainLyrics", out var pe) &&
                    pe.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(pe.GetString()))
                    return CreatePlainLyrics(pe.GetString()!);
            }
        }
        catch { }
        return null;
    }

    private List<LyricLine> CreatePlainLyrics(string plainText)
    {
        var lines = new List<LyricLine>();
        foreach (var line in plainText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                lines.Add(new LyricLine { Timestamp = TimeSpan.Zero, Text = trimmed });
        }
        return lines;
    }

    private List<LyricLine> ParseLrc(string lrc)
    {
        var lines = new List<LyricLine>();
        foreach (var line in lrc.Split('\n'))
        {
            var match = System.Text.RegularExpressions.Regex.Match(line, @"\[(\d+):(\d+)\.(\d+)\](.*)");
            if (match.Success)
            {
                var text = match.Groups[4].Value.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    lines.Add(new LyricLine
                    {
                        Timestamp = new TimeSpan(0, 0,
                            int.Parse(match.Groups[1].Value),
                            int.Parse(match.Groups[2].Value),
                            int.Parse(match.Groups[3].Value) * 10),
                        Text = text
                    });
                }
            }
        }
        return lines.OrderBy(l => l.Timestamp).ToList();
    }
}