using Castle.Core.Interfaces;
using Castle.Core.Models;
using LiteDB;

namespace Castle.Data.Repositories;

public class BlacklistRepository : IBlacklistRepository
{
    private readonly Database _database;
    private const string CollectionName = "blacklist";

    public BlacklistRepository(Database database) { _database = database; }

    public void Add(BlacklistEntry entry) { var col = _database.GetDatabase().GetCollection<BlacklistEntry>(CollectionName); if (!col.Exists(b => b.VideoId == entry.VideoId)) col.Insert(entry); }
    public void Remove(string videoId) { _database.GetDatabase().GetCollection<BlacklistEntry>(CollectionName).DeleteMany(b => b.VideoId == videoId); }
    public bool IsBlacklisted(string videoId) { return _database.GetDatabase().GetCollection<BlacklistEntry>(CollectionName).Exists(b => b.VideoId == videoId); }
    public List<BlacklistEntry> GetAll() { return _database.GetDatabase().GetCollection<BlacklistEntry>(CollectionName).FindAll().ToList(); }
}