using Castle.Core.Models;

namespace Castle.Core.Interfaces;

public interface IPlaylistRepository
{
    void Insert(Playlist playlist);
    void Update(Playlist playlist);
    void Delete(string id);
    Playlist? GetById(string id);
    List<Playlist> GetAll();
    void AddSong(string playlistId, string songId);
    void RemoveSong(string playlistId, string songId);
}