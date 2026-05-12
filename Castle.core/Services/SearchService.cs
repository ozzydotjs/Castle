using System.Net.Http;
using YoutubeExplode;
using YoutubeExplode.Search;

namespace Castle.Core.Services;

public class SearchService
{
    private readonly YoutubeClient _youtube;
    private readonly Dictionary<string, (List<SearchResult> Results, DateTime Time)> _cache = new();
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public SearchService()
    {
        var handler = new HttpClientHandler
        {
            UseProxy = false,
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };

        var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        _youtube = new YoutubeClient(httpClient);
    }

    public async Task<List<SearchResult>> SearchAsync(string query, int maxResults = 10)
    {
        var key = $"{query}_{maxResults}";

        if (_cache.TryGetValue(key, out var cached) && DateTime.Now - cached.Time < _cacheDuration)
            return cached.Results;

        var results = new List<SearchResult>();

        try
        {
            var videos = new List<VideoSearchResult>();
            await foreach (var video in _youtube.Search.GetVideosAsync(query))
            {
                videos.Add(video);
                if (videos.Count >= maxResults) break;
            }

            foreach (var video in videos)
            {
                results.Add(new SearchResult
                {
                    VideoId = video.Id.Value,
                    Title = video.Title,
                    Author = video.Author.ChannelTitle,
                    Duration = video.Duration?.ToString(@"m\:ss") ?? "?",
                    ThumbnailUrl = video.Thumbnails.FirstOrDefault()?.Url ?? ""
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
        }

        _cache[key] = (results, DateTime.Now);

        var expired = _cache.Where(kvp => DateTime.Now - kvp.Value.Time > _cacheDuration).ToList();
        foreach (var entry in expired) _cache.Remove(entry.Key);

        return results;
    }

    public async Task<List<SearchResult>> SearchMusicAsync(string query, int maxResults = 10)
    {
        var results = new List<SearchResult>();

        try
        {
            await foreach (var video in _youtube.Search.GetVideosAsync(query))
            {
                if (video.Duration >= TimeSpan.FromMinutes(1) &&
                    video.Duration <= TimeSpan.FromMinutes(15))
                {
                    results.Add(new SearchResult
                    {
                        VideoId = video.Id.Value,
                        Title = video.Title,
                        Author = video.Author.ChannelTitle,
                        Duration = video.Duration?.ToString(@"m\:ss") ?? "?",
                        ThumbnailUrl = video.Thumbnails.FirstOrDefault()?.Url ?? ""
                    });
                }

                if (results.Count >= maxResults) break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Music search error: {ex.Message}");
        }

        return results;
    }

    public async Task<List<string>> GetSuggestionsAsync(string query)
    {
        var results = new List<string>();
        try
        {
            await foreach (var video in _youtube.Search.GetVideosAsync(query))
            {
                if (!results.Contains(video.Title))
                    results.Add(video.Title);
                if (results.Count >= 5) break;
            }
        }
        catch { }
        return results;
    }
}

public class SearchResult
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
}