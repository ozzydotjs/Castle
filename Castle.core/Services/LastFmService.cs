using System.Net.Http.Json;
using System.Text.Json;

namespace Castle.Core.Services;

public class LastFmService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly Dictionary<string, List<string>> _genreCache = new();

    public LastFmService(string apiKey)
    {
        _apiKey = apiKey;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "Castle/1.0");
    }

    public async Task<List<string>> GetArtistGenresAsync(string artist)
    {
        if (string.IsNullOrWhiteSpace(artist) || artist == "Unknown Artist")
            return new List<string>();

        if (_genreCache.TryGetValue(artist.ToLower(), out var cached))
            return cached;

        try
        {
            var url = $"https://ws.audioscrobbler.com/2.0/?method=artist.gettoptags&artist={Uri.EscapeDataString(artist)}&api_key={_apiKey}&format=json";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var tags = new List<string>();
            if (doc.RootElement.TryGetProperty("toptags", out var topTags) &&
                topTags.TryGetProperty("tag", out var tagArray))
            {
                foreach (var tag in tagArray.EnumerateArray())
                {
                    if (tag.TryGetProperty("name", out var name))
                    {
                        var tagName = name.GetString()?.ToLower();
                        if (!string.IsNullOrWhiteSpace(tagName) && IsGenreTag(tagName))
                            tags.Add(tagName);
                    }
                    if (tags.Count >= 5) break;
                }
            }

            _genreCache[artist.ToLower()] = tags;
            return tags;
        }
        catch
        {
            return new List<string>();
        }
    }

    public async Task<List<string>> GetSimilarArtistsAsync(string artist)
    {
        if (string.IsNullOrWhiteSpace(artist) || artist == "Unknown Artist")
            return new List<string>();

        try
        {
            var url = $"https://ws.audioscrobbler.com/2.0/?method=artist.getsimilar&artist={Uri.EscapeDataString(artist)}&api_key={_apiKey}&format=json&limit=5";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var artists = new List<string>();
            if (doc.RootElement.TryGetProperty("similarartists", out var similar) &&
                similar.TryGetProperty("artist", out var artistArray))
            {
                foreach (var a in artistArray.EnumerateArray())
                {
                    if (a.TryGetProperty("name", out var name))
                        artists.Add(name.GetString() ?? "");
                }
            }

            return artists;
        }
        catch
        {
            return new List<string>();
        }
    }

    private bool IsGenreTag(string tag)
    {
        var nonGenres = new HashSet<string>
        {
            "seen live", "favorites", "favourite", "awesome", "amazing", "best",
            "love", "favorite artists", "all", "albums i own", "top artists",
            "5 stars", "favorite", "genius", "legend", "legends"
        };
        return !nonGenres.Contains(tag);
    }
}