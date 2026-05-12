namespace Castle.Core.Models;

public class Song
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public uint TrackNumber { get; set; }
    public string Genre { get; set; } = string.Empty;
    public int Year { get; set; }
    public bool HasCoverArt { get; set; }
    public bool IsFavorite { get; set; }
    public string? CoverArtPath { get; set; }
}