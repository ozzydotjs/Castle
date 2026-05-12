namespace Castle.Core.Models;

public class Playlist
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public List<string> SongIds { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}