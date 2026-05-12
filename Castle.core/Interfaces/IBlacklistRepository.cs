using Castle.Core.Models;

namespace Castle.Core.Interfaces;

public interface IBlacklistRepository
{
    void Add(BlacklistEntry entry);
    void Remove(string videoId);
    bool IsBlacklisted(string videoId);
    List<BlacklistEntry> GetAll();
}