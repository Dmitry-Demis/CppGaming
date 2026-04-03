using cppQuest.Server.Models;
using cppQuest.Server.Repositories;

namespace cppQuest.Server.Services;

public class GamificationService(IGamificationRepository repo)
{
    // Freeze milestones: after N cumulative streak days, earn 1 freeze
    // 3, 7, 14, 21, 35, 56, 84, 126, 189, 280 ...  (×1.5 after 21)
    private static readonly int[] FreezeMilestones = [3, 7, 14, 21, 35, 56, 84, 126, 189, 280, 420, 630];

    private static int CalcLevel(int xp) => (xp / 500) + 1;

    public async Task AddXpAsync(int userId, int amount)
    {
        var profile = await repo.GetAsync(userId);
        if (profile is null) return;
        profile.Xp += amount;
        profile.Level = CalcLevel(profile.Xp);
        await repo.SaveAsync(profile);
    }

    public async Task AddCoinsAsync(int userId, int amount)
    {
        var profile = await repo.GetAsync(userId);
        if (profile is null) return;
        profile.Coins += amount;
        await repo.SaveAsync(profile);
    }

    public async Task UpdateStreakAsync(int userId)
    {
        var profile = await repo.GetAsync(userId);
        if (profile is null) return;

        var today = DateTime.UtcNow.Date;
        var lastDate = profile.LastActivityDate.Date;

        if (lastDate == today) return; // already updated today

        if (lastDate == today.AddDays(-1))
        {
            // Consecutive day
            profile.CurrentStreak++;
            profile.TotalStreakDays++;
        }
        else
        {
            // Missed day(s) — try to use a freeze
            if (profile.FreezeCount > 0)
            {
                profile.FreezeCount--;
                profile.SnowflakeCount++; // badge: used a freeze
                // streak continues, but don't increment (missed day)
            }
            else
            {
                profile.CurrentStreak = 1;
                profile.TotalStreakDays++;
            }
        }

        profile.MaxStreak = Math.Max(profile.MaxStreak, profile.CurrentStreak);
        profile.LastActivityDate = DateTime.UtcNow;

        // Award freezes at milestones
        AwardFreezesIfDue(profile);

        await repo.SaveAsync(profile);
    }

    // Called on login to check if streak was broken (no activity yesterday and no freeze)
    public async Task<StreakCheckResult> CheckStreakOnLoginAsync(int userId)
    {
        var profile = await repo.GetAsync(userId);
        if (profile is null) return new StreakCheckResult(0, 0, 0, false, false, 0, 0);

        var today = DateTime.UtcNow.Date;
        var lastDate = profile.LastActivityDate.Date;
        bool usedFreeze = false;
        bool streakBroken = false;
        int coinsReward = 0;
        int xpReward = 0;

        if (lastDate == today)
        {
            // Already checked today — just return current state, no rewards
            return new StreakCheckResult(
                profile.CurrentStreak, profile.FreezeCount, profile.SnowflakeCount,
                false, false, 0, 0);
        }

        if (lastDate == today.AddDays(-1))
        {
            // Consecutive day — increment streak and give daily reward
            profile.CurrentStreak++;
            profile.TotalStreakDays++;
            profile.MaxStreak = Math.Max(profile.MaxStreak, profile.CurrentStreak);
            profile.LastActivityDate = DateTime.UtcNow;
            AwardFreezesIfDue(profile);

            // Daily login reward
            bool isMilestoneDay = profile.CurrentStreak % 7 == 0;
            coinsReward = isMilestoneDay ? 100 : 50;
            xpReward    = isMilestoneDay ? 50  : 10;
            profile.Coins += coinsReward;
            profile.Xp    += xpReward;
            profile.Level  = CalcLevel(profile.Xp);
        }
        else if (lastDate < today.AddDays(-1))
        {
            // Missed day(s)
            if (profile.FreezeCount > 0)
            {
                profile.FreezeCount--;
                profile.SnowflakeCount++;
                usedFreeze = true;
                profile.LastActivityDate = DateTime.UtcNow;
                // Still give daily reward (freeze saved the streak)
                bool isMilestoneDay = profile.CurrentStreak % 7 == 0;
                coinsReward = isMilestoneDay ? 100 : 50;
                xpReward    = isMilestoneDay ? 50  : 10;
                profile.Coins += coinsReward;
                profile.Xp    += xpReward;
                profile.Level  = CalcLevel(profile.Xp);
            }
            else
            {
                streakBroken = profile.CurrentStreak > 1;
                profile.CurrentStreak = 1;
                profile.TotalStreakDays++;
                profile.LastActivityDate = DateTime.UtcNow;
                // Small consolation reward on restart
                coinsReward = 50;
                xpReward    = 10;
                profile.Coins += coinsReward;
                profile.Xp    += xpReward;
                profile.Level  = CalcLevel(profile.Xp);
            }
        }

        await repo.SaveAsync(profile);

        return new StreakCheckResult(
            profile.CurrentStreak,
            profile.FreezeCount,
            profile.SnowflakeCount,
            usedFreeze,
            streakBroken,
            coinsReward,
            xpReward
        );
    }

    private static void AwardFreezesIfDue(GamificationProfile profile)
    {
        foreach (var milestone in FreezeMilestones)
        {
            // Award freeze if we just crossed this milestone
            if (profile.TotalStreakDays >= milestone &&
                profile.TotalStreakDays - 1 < milestone)
            {
                profile.FreezeCount++;
            }
        }
    }

    public async Task UnlockContentStdAsync(int userId, string std)
    {
        var profile = await repo.GetAsync(userId);
        if (profile is null) return;
        var unlocked = (profile.UnlockedContentStds ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
        unlocked.Add(std);
        profile.UnlockedContentStds = string.Join(',', unlocked);
        await repo.SaveAsync(profile);
    }

    // Slot key format: "chapter-2/fundamental-types/signed-unsigned:cmp-functions"
    public async Task<HashSet<string>> GetUnlockedSlotsAsync(int userId)
    {
        var profile = await repo.GetAsync(userId);
        if (profile is null) return [];
        return (profile.UnlockedSlots ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
    }

    public async Task<bool> UnlockSlotAsync(int userId, string slotKey, int price)
    {
        var profile = await repo.GetAsync(userId);
        if (profile is null) return false;

        var unlocked = (profile.UnlockedSlots ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

        if (unlocked.Contains(slotKey)) return false; // already unlocked

        if (profile.Coins < price) return false; // not enough coins

        profile.Coins -= price;
        unlocked.Add(slotKey);
        profile.UnlockedSlots = string.Join(',', unlocked);
        await repo.SaveAsync(profile);
        return true;
    }

    public async Task<GamificationProfile?> GetAsync(int userId) =>
        await repo.GetAsync(userId);

    public async Task SaveAsync(GamificationProfile profile) =>
        await repo.SaveAsync(profile);
}

public record StreakCheckResult(
    int CurrentStreak,
    int FreezeCount,
    int SnowflakeCount,
    bool UsedFreeze,
    bool StreakBroken,
    int CoinsReward,
    int XpReward
);
