using cppQuest.Server.Models;
using cppQuest.Server.Repositories;

namespace cppQuest.Server.Services;

/// <summary>
/// Отвечает за стрики, заморозки и ежедневные награды.
///
/// Алгоритм стрика (UpdateAsync):
///   - Если сегодня уже была активность → ничего не делаем.
///   - Если вчера была активность → стрик +1, TotalStreakDays +1.
///   - Если пропущен день и есть заморозка → тратим заморозку, стрик сохраняется.
///   - Если пропущен день и заморозок нет → стрик сбрасывается в 1.
///   - После каждого изменения проверяем milestone-заморозки.
///
/// Алгоритм CheckStreakOnLoginAsync:
///   Аналогичен UpdateAsync, но дополнительно начисляет ежедневную награду:
///   - Обычный день: +50 монет, +10 XP.
///   - Каждые 7 дней (milestone): +100 монет, +50 XP.
///
/// Milestone-заморозки (FreezeMilestones):
///   При достижении TotalStreakDays = N выдаётся 1 заморозка.
///   Прогрессия: 3, 7, 14, 21, 35, 56, 84, 126, 189, 280, 420, 630 дней.
/// </summary>
public class StreakService(IGamificationRepository repo)
{
    private static readonly int[] FreezeMilestones =
        [3, 7, 14, 21, 35, 56, 84, 126, 189, 280, 420, 630];

    /// <summary>
    /// Обновляет стрик при любой активности (чтение, тест).
    /// Не начисляет ежедневную награду — только обновляет стрик.
    /// </summary>
    public async Task UpdateAsync(int userId)
    {
        var profile = await repo.GetAsync(userId);
        if (profile is null) return;

        var today    = DateTime.UtcNow.Date;
        var lastDate = profile.LastActivityDate.Date;

        if (lastDate == today) return; // уже обновляли сегодня

        if (lastDate == today.AddDays(-1))
        {
            profile.CurrentStreak++;
            profile.TotalStreakDays++;
        }
        else
        {
            // Пропущен день — пробуем заморозку
            if (profile.FreezeCount > 0)
            {
                profile.FreezeCount--;
                profile.SnowflakeCount++;
                // стрик не растёт, но и не сбрасывается
            }
            else
            {
                profile.CurrentStreak = 1;
                profile.TotalStreakDays++;
            }
        }

        profile.MaxStreak        = Math.Max(profile.MaxStreak, profile.CurrentStreak);
        profile.LastActivityDate = DateTime.UtcNow;
        AwardFreezesIfDue(profile);

        await repo.SaveAsync(profile);
    }

    /// <summary>
    /// Проверяет стрик при входе и начисляет ежедневную награду.
    /// Вызывается один раз в день при логине.
    /// </summary>
    public async Task<StreakCheckResult> CheckOnLoginAsync(int userId)
    {
        var profile = await repo.GetAsync(userId);
        if (profile is null) return StreakCheckResult.Empty;

        var today    = DateTime.UtcNow.Date;
        var lastDate = profile.LastActivityDate.Date;

        // Уже проверяли сегодня — возвращаем текущее состояние без наград
        if (lastDate == today)
            return new StreakCheckResult(profile.CurrentStreak, profile.FreezeCount,
                profile.SnowflakeCount, false, false, 0, 0);

        bool usedFreeze   = false;
        bool streakBroken = false;
        int  coinsReward  = 0;
        int  xpReward     = 0;

        if (lastDate == today.AddDays(-1))
        {
            // Последовательный день
            profile.CurrentStreak++;
            profile.TotalStreakDays++;
            profile.MaxStreak        = Math.Max(profile.MaxStreak, profile.CurrentStreak);
            profile.LastActivityDate = DateTime.UtcNow;
            AwardFreezesIfDue(profile);
            (coinsReward, xpReward) = DailyReward(profile.CurrentStreak);
        }
        else
        {
            // Пропущен день
            if (profile.FreezeCount > 0)
            {
                profile.FreezeCount--;
                profile.SnowflakeCount++;
                usedFreeze               = true;
                profile.LastActivityDate = DateTime.UtcNow;
                (coinsReward, xpReward)  = DailyReward(profile.CurrentStreak);
            }
            else
            {
                streakBroken             = profile.CurrentStreak > 1;
                profile.CurrentStreak    = 1;
                profile.TotalStreakDays++;
                profile.LastActivityDate = DateTime.UtcNow;
                // Утешительная награда при сбросе
                coinsReward = 50;
                xpReward    = 10;
            }
        }

        XpService.Apply(profile, xpReward);
        CurrencyService.ApplyCoins(profile, coinsReward);

        await repo.SaveAsync(profile);

        return new StreakCheckResult(profile.CurrentStreak, profile.FreezeCount,
            profile.SnowflakeCount, usedFreeze, streakBroken, coinsReward, xpReward);
    }

    // ── Вспомогательные ──────────────────────────────────────────────────────

    /// <summary>
    /// Ежедневная награда: каждые 7 дней — повышенная.
    /// Возвращает (coins, xp).
    /// </summary>
    private static (int coins, int xp) DailyReward(int currentStreak) =>
        currentStreak % 7 == 0 ? (100, 50) : (50, 10);

    /// <summary>
    /// Выдаёт заморозку, если TotalStreakDays только что пересёк milestone.
    /// Проверяем: было (days-1) < milestone ≤ days.
    /// </summary>
    private static void AwardFreezesIfDue(GamificationProfile profile)
    {
        foreach (var milestone in FreezeMilestones)
        {
            if (profile.TotalStreakDays >= milestone &&
                profile.TotalStreakDays - 1 < milestone)
                profile.FreezeCount++;
        }
    }
}

/// <summary>Результат проверки стрика при логине.</summary>
public record StreakCheckResult(
    int CurrentStreak,
    int FreezeCount,
    int SnowflakeCount,
    bool UsedFreeze,
    bool StreakBroken,
    int CoinsReward,
    int XpReward)
{
    public static readonly StreakCheckResult Empty = new(0, 0, 0, false, false, 0, 0);
}
