using Castle.Core.Models;

namespace Castle.Core.Services;

public class MetadataService
{
    private readonly LyricsService _lyricsService;

    public MetadataService(LyricsService lyricsService)
    {
        _lyricsService = lyricsService;
    }

    public void WriteMetadata(string filePath, string title, string artist)
    {
        try
        {
            using var tagFile = TagLib.File.Create(filePath);
            tagFile.Tag.Title = title;
            tagFile.Tag.Performers = new[] { artist };
            tagFile.Save();
        }
        catch { }
    }

    public async Task EmbedLyricsAsync(string filePath, string title, string artist)
    {
        try
        {
            var lyrics = await _lyricsService.GetLyricsAsync(title, artist);
            if (lyrics == null || lyrics.Count == 0) return;

            using var tagFile = TagLib.File.Create(filePath);

            var hasTimestamps = lyrics.Any(l => l.Timestamp > TimeSpan.Zero);

            if (hasTimestamps)
            {
                var lrc = string.Join("\n", lyrics.Select(l =>
                    $"[{l.Timestamp.Minutes:D2}:{l.Timestamp.Seconds:D2}.{l.Timestamp.Milliseconds / 10:D2}]{l.Text}"));
                tagFile.Tag.Lyrics = lrc;
            }
            else
            {
                tagFile.Tag.Lyrics = string.Join("\n", lyrics.Select(l => l.Text));
            }

            tagFile.Save();
            System.Diagnostics.Debug.WriteLine($"[Metadata] Lyrics embedded for: {title}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Metadata] Lyrics embed failed: {ex.Message}");
        }
    }
}