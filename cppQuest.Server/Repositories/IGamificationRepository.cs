using cppQuest.Server.Models;

namespace cppQuest.Server.Repositories;

public interface IGamificationRepository
{
    Task<GamificationProfile?> GetAsync(int userId);
    Task SaveAsync(GamificationProfile profile);
    Task CreateDefaultAsync(int userId);
}
