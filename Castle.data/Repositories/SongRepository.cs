using Castle.Core.Interfaces;
using Castle.Core.Models;
using LiteDB;

namespace Castle.Data.Repositories;

public class SongRepository : ISongRepository
{
    private readonly Database _database;
    private const string CollectionName = "songs";

    public SongRepository(Database database)
    {
        _database = database;

        // Ensure unique index on FilePath to prevent duplicates at database level
        var coll = _database.GetDatabase().GetCollection<Song>(CollectionName);
        coll.EnsureIndex(s => s.FilePath, unique: true);
    }

    public void Insert(Song song)
    {
        song.Id = Guid.NewGuid().ToString();

        // Check if song with this path already exists
        var existing = GetByFilePath(song.FilePath);
        if (existing != null)
        {
            // Update existing instead of creating duplicate
            song.Id = existing.Id;
            Update(song);
            return;
        }

        _database.GetDatabase().GetCollection<Song>(CollectionName).Insert(song);
    }

    public void InsertBatch(IEnumerable<Song> songs)
    {
        foreach (var song in songs)
        {
            Insert(song); // Use Insert which now checks for duplicates
        }
    }

    public List<Song> GetAll() =>
        _database.GetDatabase().GetCollection<Song>(CollectionName).FindAll().ToList();

    public List<Song> GetFavorites() =>
        _database.GetDatabase().GetCollection<Song>(CollectionName).Find(s => s.IsFavorite).ToList();

    public Song? GetById(string id) =>
        _database.GetDatabase().GetCollection<Song>(CollectionName).FindById(id);

    public Song? GetByFilePath(string filePath) =>
        _database.GetDatabase().GetCollection<Song>(CollectionName).FindOne(s => s.FilePath == filePath);

    public List<Song> Search(string query)
    {
        var lower = query.ToLower();
        return _database.GetDatabase().GetCollection<Song>(CollectionName)
            .Find(s => s.Title.ToLower().Contains(lower) ||
                       s.Artist.ToLower().Contains(lower) ||
                       s.Album.ToLower().Contains(lower))
            .ToList();
    }

    public void Update(Song song) =>
        _database.GetDatabase().GetCollection<Song>(CollectionName).Update(song);

    public void DeleteAll() =>
        _database.GetDatabase().GetCollection<Song>(CollectionName).DeleteAll();

    public void Delete(string id) =>
        _database.GetDatabase().GetCollection<Song>(CollectionName).Delete(id);

    // One-time cleanup: remove any existing duplicates (keep the first inserted)
    public int RemoveDuplicates()
    {
        var all = GetAll();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new List<string>();

        foreach (var song in all)
        {
            if (!seen.Add(song.FilePath))
            {
                duplicates.Add(song.Id);
            }
        }

        foreach (var id in duplicates)
        {
            Delete(id);
        }

        return duplicates.Count;
    }
}