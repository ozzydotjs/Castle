using LiteDB;

namespace Castle.Data;

public class Database
{
    private readonly LiteDatabase _db;

    public Database(string dbPath)
    {
        _db = new LiteDatabase($"Filename={dbPath};Connection=direct");
    }

    public ILiteDatabase GetDatabase() => _db;
}