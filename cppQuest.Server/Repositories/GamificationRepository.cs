using Microsoft.EntityFrameworkCore;
using cppQuest.Server.Models;

namespace cppQuest.Server.Repositories;

public class GamificationRepository(AppDbContext db) : IGamificationRepository
{
    public async Task<GamificationProfile?> GetAsync(int userId) =>
        await db.GamificationProfiles.FindAsync(userId);

    public async Task SaveAsync(GamificationProfile profile)
    {
        db.GamificationProfiles.Update(profile);
        await db.SaveChangesAsync();
    }

    public async Task CreateDefaultAsync(int userId)
    {
        db.GamificationProfiles.Add(new GamificationProfile
        {
            UserId = userId,
            Xp = 0,
            Level = 1,
            Coins = 0,
            CurrentStreak = 0,
            MaxStreak = 0,
            LastActivityDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
