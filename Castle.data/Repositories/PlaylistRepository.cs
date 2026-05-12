using Castle.Core.Interfaces;
using Castle.Core.Models;

namespace Castle.Data.Repositories;

public class PlaylistRepository : IPlaylistRepository
{
    private readonly Database _database;
    private const string CollectionName = "playlists";

    public PlaylistRepository(Database database)
    {
        _database = database;
    }

    public void Insert(Playlist playlist)
    {
        if (playlist == null)
        {
            return;
        }

        var col = _database.GetDatabase().GetCollection<Playlist>(CollectionName);

        if (string.IsNullOrWhiteSpace(playlist.Id))
        {
            playlist.Id = Guid.NewGuid().ToString();
        }

        playlist.Name = playlist.Name.Trim();

        if (playlist.CreatedAt == default)
        {
            playlist.CreatedAt = DateTime.Now;
        }

        col.Insert(playlist);
    }

    public void Update(Playlist playlist)
    {
        if (playlist == null || string.IsNullOrWhiteSpace(playlist.Id))
        {
            return;
        }

        playlist.Name = playlist.Name.Trim();

        var col = _database.GetDatabase().GetCollection<Playlist>(CollectionName);
        col.Update(playlist);
    }

    public void Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var col = _database.GetDatabase().GetCollection<Playlist>(CollectionName);
        col.Delete(id);
    }

    public Playlist? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var col = _database.GetDatabase().GetCollection<Playlist>(CollectionName);
        return col.FindById(id);
    }

    public List<Playlist> GetAll()
    {
        var col = _database.GetDatabase().GetCollection<Playlist>(CollectionName);

        return col
            .FindAll()
            .OrderBy(p => p.CreatedAt)
            .ToList();
    }

    public void AddSong(string playlistId, string songId)
    {
        if (string.IsNullOrWhiteSpace(playlistId) || string.IsNullOrWhiteSpace(songId))
        {
            return;
        }

        var playlist = GetById(playlistId);

        if (playlist == null)
        {
            return;
        }

        var alreadyExists = playlist.SongIds.Any(id =>
            string.Equals(id, songId, StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
        {
            return;
        }

        playlist.SongIds.Add(songId);
        Update(playlist);
    }

    public void RemoveSong(string playlistId, string songId)
    {
        if (string.IsNullOrWhiteSpace(playlistId) || string.IsNullOrWhiteSpace(songId))
        {
            return;
        }

        var playlist = GetById(playlistId);

        if (playlist == null)
        {
            return;
        }

        playlist.SongIds.RemoveAll(id =>
            string.Equals(id, songId, StringComparison.OrdinalIgnoreCase));

        Update(playlist);
    }
}