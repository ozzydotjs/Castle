using LiteDB;

namespace Castle.Data;

public class Database
{
    private readonly LiteDatabase _db;

    public Database(string dbPath)
    {
        if (File.Exists(dbPath))
        {
            var dbSize = new FileInfo(dbPath).Length;
            if (dbSize > 50 * 1024 * 1024)
            {
                try
                {
                    using var compactDb = new LiteDatabase($"Filename={dbPath};Connection=direct");
                    compactDb.Rebuild();
                }
                catch { }
            }
        }

        _db = new LiteDatabase($"Filename={dbPath};Connection=direct");
    }

    public ILiteDatabase GetDatabase() => _db;
}