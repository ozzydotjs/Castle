using Castle.Core.Interfaces;
using Castle.Core.Models;
using LiteDB;

namespace Castle.Data.Repositories;

public class RecentlyPlayedRepository : IRecentlyPlayedRepository
{
    private readonly Database _database;
    private const string CollectionName = "recently_played";

    public RecentlyPlayedRepository(Database database) { _database = database; }

    public void Add(RecentlyPlayedEntry entry)
    {
        var col = _database.GetDatabase().GetCollection<RecentlyPlayedEntry>(CollectionName);
        col.DeleteMany(r => r.FilePath == entry.FilePath);
        col.Insert(entry);
        var all = col.FindAll().OrderByDescending(r => r.PlayedAt).ToList();
        if (all.Count > 100) { foreach (var item in all.Skip(100)) col.Delete(item.Id); }
    }

    public List<RecentlyPlayedEntry> GetAll(int limit = 50)
    {
        return _database.GetDatabase().GetCollection<RecentlyPlayedEntry>(CollectionName).FindAll().OrderByDescending(r => r.PlayedAt).Take(limit).ToList();
    }

    public void Clear() { _database.GetDatabase().GetCollection<RecentlyPlayedEntry>(CollectionName).DeleteAll(); }
}