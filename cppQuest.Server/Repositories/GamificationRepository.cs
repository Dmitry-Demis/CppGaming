using Microsoft.EntityFrameworkCore;
using cppQuest.Server.Models;
using cppQuest.Server.Services;

namespace cppQuest.Server.Repositories;

public class GamificationRepository(AppDbContext db) : IGamificationRepository
{
    public async Task<GamificationProfile?> GetAsync(int userId)
    {
        var profile = await db.GamificationProfiles.FindAsync(userId);
        if (profile is null) return null;

        // Исправляем рассинхронизацию XP/Level после ручного изменения в БД
        if (XpService.Normalize(profile))
            await SaveAsync(profile);

        return profile;
    }

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
