namespace Castle.Core.Models;

public class RecentlyPlayedEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SongId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? CoverArtPath { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.Now;
}