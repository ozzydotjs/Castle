using Castle.Core.Interfaces;

namespace Castle.Core.Services;

public class DownloadQueueItem
{
    public string VideoUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public string? PlaylistId { get; set; }
}

public class DownloadQueueService
{
    private readonly DownloadService _downloadService;
    private readonly ISongRepository _songRepo;
    private readonly IPlaylistRepository _playlistRepo;
    private readonly List<DownloadQueueItem> _queue = new();
    private bool _isProcessing;

    public event Action? QueueChanged;
    public event Action<DownloadQueueItem>? ItemStatusChanged;

    public IReadOnlyList<DownloadQueueItem> Queue => _queue;
    public bool IsProcessing => _isProcessing;

    public DownloadQueueService(
        DownloadService downloadService,
        ISongRepository songRepo,
        IPlaylistRepository playlistRepo)
    {
        _downloadService = downloadService;
        _songRepo = songRepo;
        _playlistRepo = playlistRepo;
        _downloadService.DownloadComplete += OnDownloadComplete;
    }

    public void AddToQueue(string videoUrl, string title, string artist, string? playlistId = null)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
        {
            return;
        }

        if (_queue.Any(q => q.VideoUrl == videoUrl))
        {
            return;
        }

        _queue.Add(new DownloadQueueItem
        {
            VideoUrl = videoUrl,
            Title = string.IsNullOrWhiteSpace(title) ? "Unknown Title" : title.Trim(),
            Artist = string.IsNullOrWhiteSpace(artist) ? "Unknown Artist" : artist.Trim(),
            Status = "Queued",
            PlaylistId = playlistId
        });

        QueueChanged?.Invoke();

        if (!_isProcessing)
        {
            _ = ProcessQueueAsync();
        }
    }

    public void AddBatchToQueue(IEnumerable<(string videoUrl, string title, string artist)> items)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.videoUrl))
            {
                continue;
            }

            if (_queue.Any(q => q.VideoUrl == item.videoUrl))
            {
                continue;
            }

            _queue.Add(new DownloadQueueItem
            {
                VideoUrl = item.videoUrl,
                Title = string.IsNullOrWhiteSpace(item.title) ? "Unknown Title" : item.title.Trim(),
                Artist = string.IsNullOrWhiteSpace(item.artist) ? "Unknown Artist" : item.artist.Trim(),
                Status = "Queued"
            });
        }

        QueueChanged?.Invoke();

        if (!_isProcessing)
        {
            _ = ProcessQueueAsync();
        }
    }

    public void RemoveFromQueue(string videoUrl)
    {
        var item = _queue.FirstOrDefault(q => q.VideoUrl == videoUrl && q.Status == "Queued");

        if (item != null)
        {
            _queue.Remove(item);
            QueueChanged?.Invoke();
        }
    }

    public void ClearCompleted()
    {
        _queue.RemoveAll(q => q.Status == "Complete" || q.Status == "Failed");
        QueueChanged?.Invoke();
    }

    private async Task ProcessQueueAsync()
    {
        _isProcessing = true;

        while (_queue.Any(q => q.Status == "Queued"))
        {
            var item = _queue.First(q => q.Status == "Queued");

            item.Status = "Downloading";
            ItemStatusChanged?.Invoke(item);
            QueueChanged?.Invoke();

            try
            {
                await _downloadService.DownloadAsync(item.VideoUrl, item.Title, item.Artist);
                item.Status = "Complete";
            }
            catch
            {
                item.Status = "Failed";
            }

            ItemStatusChanged?.Invoke(item);
            QueueChanged?.Invoke();

            await Task.Delay(5000);
        }

        _isProcessing = false;
    }

    private async void OnDownloadComplete()
    {
        System.Diagnostics.Debug.WriteLine($"[Queue] OnDownloadComplete fired. Items: {_queue.Count}");

        var downloadFolder = _downloadService.DownloadPath;

        Directory.CreateDirectory(downloadFolder);

        var completedItems = _queue
            .Where(q => q.Status == "Complete" || q.Status == "Downloading")
            .ToList();

        System.Diagnostics.Debug.WriteLine($"[Queue] Completed items: {completedItems.Count}");

        foreach (var item in completedItems)
        {
            try
            {
                item.Status = "Complete";

                var finalPath = FindNewestLikelyDownload(downloadFolder, item.Title, item.Artist);

                if (string.IsNullOrWhiteSpace(finalPath) || !File.Exists(finalPath))
                {
                    continue;
                }

                try
                {
                    using var tagFile = TagLib.File.Create(finalPath);
                    tagFile.Tag.Title = item.Title;
                    tagFile.Tag.Performers = new[] { item.Artist };
                    tagFile.Save();
                }
                catch
                {
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var lyricsService = new LyricsService();
                        var metadataService = new MetadataService(lyricsService);
                        await metadataService.EmbedLyricsAsync(finalPath, item.Title, item.Artist);
                    }
                    catch
                    {
                    }
                });

                item.VideoUrl = finalPath;
                System.Diagnostics.Debug.WriteLine($"[Queue] Processed: {item.Artist} - {item.Title} at {finalPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Queue] Error processing item: {ex.Message}");
            }
        }

        try
        {
            var scanner = _downloadService.GetScanner();

            if (scanner != null)
            {
                System.Diagnostics.Debug.WriteLine("[Queue] Running scanner...");

                var songs = await scanner.ScanFolderAsync(downloadFolder);
                _songRepo.InsertBatch(songs);

                System.Diagnostics.Debug.WriteLine("[Queue] Songs inserted into database");

                foreach (var item in completedItems.Where(q => !string.IsNullOrEmpty(q.PlaylistId)))
                {
                    var filePath = item.VideoUrl;

                    System.Diagnostics.Debug.WriteLine($"[Queue] Looking for file: {filePath}");

                    var song = _songRepo.GetByFilePath(filePath);

                    System.Diagnostics.Debug.WriteLine($"[Queue] Found song: {(song != null ? song.Title : "NULL")}");

                    if (song != null && !string.IsNullOrEmpty(item.PlaylistId))
                    {
                        _playlistRepo.AddSong(item.PlaylistId!, song.Id);
                        System.Diagnostics.Debug.WriteLine($"[Queue] Added to playlist {item.PlaylistId}: {song.Title}");
                    }
                }
            }
        }
        catch
        {
        }

        QueueChanged?.Invoke();
    }

    private static string? FindNewestLikelyDownload(string downloadFolder, string title, string artist)
    {
        if (!Directory.Exists(downloadFolder))
        {
            return null;
        }

        var safeFileName = DownloadService.SanitizeFileName($"{artist} - {title}");

        var matchingFiles = Directory.GetFiles(downloadFolder, $"{safeFileName}*.mp3")
            .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
            .ToList();

        if (matchingFiles.Count > 0)
        {
            return matchingFiles.First();
        }

        return Directory.GetFiles(downloadFolder, "*.mp3")
            .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
            .FirstOrDefault();
    }
}