using Castle.Core.Interfaces;
using Castle.Core.Models;
using LiteDB;

namespace Castle.Data.Repositories;

public class SongRepository : ISongRepository
{
    private readonly Database _database;
    private const string CollectionName = "songs";

    public SongRepository(Database database) { _database = database; }

    public void Insert(Song song) { song.Id = Guid.NewGuid().ToString(); _database.GetDatabase().GetCollection<Song>(CollectionName).Insert(song); }
    public void InsertBatch(IEnumerable<Song> songs) { _database.GetDatabase().GetCollection<Song>(CollectionName).InsertBulk(songs); }
    public List<Song> GetAll() { return _database.GetDatabase().GetCollection<Song>(CollectionName).FindAll().ToList(); }
    public List<Song> GetFavorites() { return _database.GetDatabase().GetCollection<Song>(CollectionName).Find(s => s.IsFavorite).ToList(); }
    public Song? GetById(string id) { return _database.GetDatabase().GetCollection<Song>(CollectionName).FindById(id); }
    public Song? GetByFilePath(string filePath) { return _database.GetDatabase().GetCollection<Song>(CollectionName).FindOne(s => s.FilePath == filePath); }
    public List<Song> Search(string query) { var lower = query.ToLower(); return _database.GetDatabase().GetCollection<Song>(CollectionName).Find(s => s.Title.ToLower().Contains(lower) || s.Artist.ToLower().Contains(lower) || s.Album.ToLower().Contains(lower)).ToList(); }
    public void Update(Song song) { _database.GetDatabase().GetCollection<Song>(CollectionName).Update(song); }
    public void DeleteAll() { _database.GetDatabase().GetCollection<Song>(CollectionName).DeleteAll(); }
}