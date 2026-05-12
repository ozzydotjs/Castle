using System.Net.Http.Json;
using System.Text.Json;
using Castle.Core.Models;

namespace Castle.Core.Services;

public class LyricsService
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, List<LyricLine>> _cache = new();

    public LyricsService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        _http.DefaultRequestHeaders.Add("User-Agent", "Castle/1.0");
    }

    public async Task<List<LyricLine>?> GetLyricsAsync(string title, string artist)
    {
        var cacheKey = $"{title}|{artist}";

        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached.Count == 0 ? null : cached;

        try
        {
            List<LyricLine>? result = await TryGetLyrics(title, artist)
                ?? await TryGetLyrics(title, "")
                ?? await SearchLyrics(title);

            _cache[cacheKey] = result ?? new List<LyricLine>();
            return result;
        }
        catch
        {
            _cache[cacheKey] = new List<LyricLine>();
            return null;
        }
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