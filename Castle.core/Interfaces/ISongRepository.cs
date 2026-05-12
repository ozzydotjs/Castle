using Castle.Core.Models;

namespace Castle.Core.Interfaces;

public interface ISongRepository
{
    void Insert(Song song);
    void InsertBatch(IEnumerable<Song> songs);
    List<Song> GetAll();
    List<Song> GetFavorites();
    Song? GetById(string id);
    Song? GetByFilePath(string filePath);
    List<Song> Search(string query);
    void Update(Song song);
    void DeleteAll();
}