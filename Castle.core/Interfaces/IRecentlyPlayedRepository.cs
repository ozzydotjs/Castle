using Castle.Core.Models;

namespace Castle.Core.Interfaces;

public interface IRecentlyPlayedRepository
{
    void Add(RecentlyPlayedEntry entry);
    List<RecentlyPlayedEntry> GetAll(int limit = 50);
    void Clear();
}