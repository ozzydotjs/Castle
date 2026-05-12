using Castle.Core.Interfaces;
using Castle.Core.Models;

namespace Castle.Core.Services;

public class RecommendationService
{
    private readonly ISongRepository _songRepo;
    private readonly SearchService _searchService;
    private readonly LastFmService _lastFm;
    private readonly IBlacklistRepository _blacklistRepo;

    private static readonly HashSet<string> NonMusicTitleWords = new()
    {
        "tutorial", "how to", "diy", "experiment", "lecture", "anatomy",
        "explained", "guide", "review", "unboxing", "reaction", "podcast",
        "interview", "lesson", "course", "basics", "introduction", "summary",
        "walkthrough", "analysis", "documentary", "behind the scenes"
    };

    public RecommendationService(ISongRepository songRepo, SearchService searchService, LastFmService lastFm, IBlacklistRepository blacklistRepo)
    {
        _songRepo = songRepo;
        _searchService = searchService;
        _lastFm = lastFm;
        _blacklistRepo = blacklistRepo;
    }
    public List<Song> GetLibrarySongs()
    {
        return _songRepo.GetAll();
    }
    public async Task<List<RecommendedTrack>> GetRecommendationsAsync(int count = 10)
    {
        var songs = _songRepo.GetAll();
        if (songs.Count == 0) return new List<RecommendedTrack>();

        var topArtists = songs
            .Where(s => s.Artist != "Unknown Artist")
            .GroupBy(s => s.Artist)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key)
            .ToList();

        var allResults = new List<RecommendedTrack>();
        var seenIds = new HashSet<string>();

        foreach (var artist in topArtists)
        {
            var genres = await _lastFm.GetArtistGenresAsync(artist);
            var topGenre = genres.FirstOrDefault();

            var query = !string.IsNullOrWhiteSpace(topGenre)
                ? $"{artist} {topGenre}"
                : artist;

            var results = await _searchService.SearchMusicAsync(query, 5);
            foreach (var r in results)
            {
                if (seenIds.Contains(r.VideoId) || _blacklistRepo.IsBlacklisted(r.VideoId))
                    continue;

                if (NonMusicTitleWords.Any(w => r.Title.ToLower().Contains(w)))
                    continue;

                seenIds.Add(r.VideoId);
                allResults.Add(new RecommendedTrack
                {
                    VideoId = r.VideoId,
                    Title = r.Title,
                    Author = r.Author,
                    Duration = r.Duration,
                    ThumbnailUrl = r.ThumbnailUrl,
                    Reason = !string.IsNullOrWhiteSpace(topGenre)
                        ? $"Because you listen to {artist} ({topGenre})"
                        : $"Because you listen to {artist}"
                });
            }

            var similar = await _lastFm.GetSimilarArtistsAsync(artist);
            foreach (var simArtist in similar.Take(2))
            {
                var simResults = await _searchService.SearchMusicAsync(simArtist, 3);
                foreach (var r in simResults)
                {
                    if (seenIds.Contains(r.VideoId) || _blacklistRepo.IsBlacklisted(r.VideoId))
                        continue;

                    if (NonMusicTitleWords.Any(w => r.Title.ToLower().Contains(w)))
                        continue;

                    seenIds.Add(r.VideoId);
                    allResults.Add(new RecommendedTrack
                    {
                        VideoId = r.VideoId,
                        Title = r.Title,
                        Author = r.Author,
                        Duration = r.Duration,
                        ThumbnailUrl = r.ThumbnailUrl,
                        Reason = $"Similar to {artist}"
                    });
                }
            }
        }

        allResults = FilterDuplicateTitles(allResults, songs);

        var random = new Random();
        return allResults.OrderBy(_ => random.Next()).Take(count).ToList();
    }

    private List<RecommendedTrack> FilterDuplicateTitles(List<RecommendedTrack> recommendations, List<Song> librarySongs)
    {
        var libraryTitles = librarySongs
            .Select(s => s.Title.ToLower().Trim())
            .Where(t => t.Length > 3)
            .ToList();

        return recommendations.Where(rec =>
        {
            var recTitle = rec.Title.ToLower().Trim();

            foreach (var libTitle in libraryTitles)
            {
                if (recTitle.Contains(libTitle) || libTitle.Contains(recTitle))
                    return false;

                var recWords = recTitle.Split(' ').Where(w => w.Length > 2).ToHashSet();
                var libWords = libTitle.Split(' ').Where(w => w.Length > 2).ToHashSet();

                if (recWords.Count > 0 && libWords.Count > 0)
                {
                    var overlap = recWords.Intersect(libWords).Count();
                    var similarity = (double)overlap / Math.Min(recWords.Count, libWords.Count);

                    if (similarity > 0.7)
                        return false;
                }
            }

            return true;
        }).ToList();
    }
}

public class RecommendedTrack
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}