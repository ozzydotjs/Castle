using Castle.Core.Interfaces;
using Castle.Core.Models;

namespace Castle.Core.Services;

public class DiscoverService
{
    private readonly RecommendationService _recommendationService;
    private readonly SearchService _searchService;
    private List<RecommendedTrack>? _cachedDiscover;
    private List<PopularPlaylist>? _cachedPlaylists;
    private List<PopularPlaylist>? _cachedForYou;
    private readonly Random _random = new();

    public DiscoverService(RecommendationService recommendationService, SearchService searchService)
    {
        _recommendationService = recommendationService;
        _searchService = searchService;
    }

    public async Task<List<RecommendedTrack>> GetDailyDiscoverAsync(int count = 8)
    {
        if (_cachedDiscover != null)
            return _cachedDiscover;

        _cachedDiscover = await _recommendationService.GetRecommendationsAsync(count);
        return _cachedDiscover;
    }

    public async Task<List<PopularPlaylist>> GetPopularPlaylistsAsync()
    {
        if (_cachedPlaylists != null)
            return _cachedPlaylists;

        var allQueries = new List<(string name, string query)>
        {
            ("Today's Top Hits", "Top Hits 2025"),
            ("RapCaviar", "Rap hits 2025"),
            ("Rock Classics", "Best rock songs"),
            ("Chill Vibes", "Chill music mix"),
            ("Workout Energy", "Workout music motivation"),
            ("Indie Discovery", "Indie music 2025"),
            ("EDM Bangers", "EDM party mix"),
            ("Lo-Fi Beats", "Lofi hip hop"),
            ("Latin Heat", "Latin music hits"),
            ("R&B Slow Jams", "R&B slow jams"),
            ("Country Roads", "Country music hits"),
            ("Jazz Lounge", "Jazz lounge mix"),
            ("Throwback Hits", "Throwback songs 2000s"),
            ("Acoustic Covers", "Acoustic covers of popular songs"),
            ("K-Pop Hits", "K-Pop hits 2025")
        };

        var selected = allQueries.OrderBy(_ => _random.Next()).Take(6).ToList();

        _cachedPlaylists = new List<PopularPlaylist>();
        foreach (var item in selected)
        {
            var results = await _searchService.SearchAsync(item.query + " playlist", 1);
            _cachedPlaylists.Add(new PopularPlaylist
            {
                Name = item.name,
                Query = item.query + " playlist",
                ThumbnailUrl = results.Count > 0 ? results[0].ThumbnailUrl : "",
                IsPersonalized = false
            });
        }

        return _cachedPlaylists;
    }

    public async Task<List<PopularPlaylist>> GetPlaylistsForYouAsync()
    {
        if (_cachedForYou != null)
            return _cachedForYou;

        var songs = _recommendationService.GetLibrarySongs();
        _cachedForYou = new List<PopularPlaylist>();

        var topArtists = songs
            .Where(s => s.Artist != "Unknown Artist")
            .GroupBy(s => s.Artist)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key)
            .ToList();

        var topGenres = songs
            .Where(s => !string.IsNullOrWhiteSpace(s.Genre))
            .SelectMany(s => s.Genre.Split(',').Select(g => g.Trim()))
            .GroupBy(g => g)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key)
            .ToList();

        // Add artist-based playlists
        foreach (var artist in topArtists)
        {
            var query = $"similar to {artist}";
            var results = await _searchService.SearchAsync(query, 1);
            _cachedForYou.Add(new PopularPlaylist
            {
                Name = $"More Like {artist}",
                Query = query,
                ThumbnailUrl = results.Count > 0 ? results[0].ThumbnailUrl : "",
                IsPersonalized = true
            });
        }

        // Add genre-based playlists
        foreach (var genre in topGenres)
        {
            var query = $"{genre} music hits";
            var results = await _searchService.SearchAsync(query, 1);
            _cachedForYou.Add(new PopularPlaylist
            {
                Name = $"{genre} Mix",
                Query = query,
                ThumbnailUrl = results.Count > 0 ? results[0].ThumbnailUrl : "",
                IsPersonalized = true
            });
        }

        // Add some curated personal playlists
        var curated = new List<(string name, string query)>
        {
            ("On Repeat", "popular music 2025"),
            ("Discover Weekly", "new music this week"),
            ("Chill Sunday", "chill acoustic weekend vibes"),
            ("Late Night Vibes", "late night chill music")
        };

        foreach (var item in curated.OrderBy(_ => _random.Next()).Take(4 - _cachedForYou.Count))
        {
            var results = await _searchService.SearchAsync(item.query, 1);
            _cachedForYou.Add(new PopularPlaylist
            {
                Name = item.name,
                Query = item.query,
                ThumbnailUrl = results.Count > 0 ? results[0].ThumbnailUrl : "",
                IsPersonalized = true
            });
        }

        // Shuffle everything
        _cachedForYou = _cachedForYou.OrderBy(_ => _random.Next()).Take(4).ToList();

        return _cachedForYou;
    }

    public void RefreshCache()
    {
        _cachedDiscover = null;
        _cachedPlaylists = null;
        _cachedForYou = null;
    }
}

public class PopularPlaylist
{
    public string Name { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public bool IsPersonalized { get; set; }
}