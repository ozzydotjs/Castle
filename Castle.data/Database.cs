using LiteDB;

namespace Castle.Data;

public class Database
{
    private readonly LiteDatabase _db;

    public Database(string dbPath)
    {
        // Compact database if it's over 50MB
        if (File.Exists(dbPath))
        {
            var dbSize = new FileInfo(dbPath).Length;
            if (dbSize > 50 * 1024 * 1024) // 50MB
            {
                try
                {
                    using var compactDb = new LiteDatabase($"Filename={dbPath};Connection=direct");
                    compactDb.Rebuild();
                }
                catch
                {
                    // If rebuild fails, just open normally
                }
            }
        }

        _db = new LiteDatabase($"Filename={dbPath};Connection=direct");
    }

    public ILiteDatabase GetDatabase() => _db;
}