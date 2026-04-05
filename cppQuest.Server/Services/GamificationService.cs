using cppQuest.Server.Models;
using cppQuest.Server.Repositories;

namespace cppQuest.Server.Services;

/// <summary>
/// Фасад геймификации — собирает XpService, CurrencyService и StreakService.
/// Эндпоинты и другие сервисы работают через этот класс,
/// не зная о деталях реализации каждого под-сервиса.
/// </summary>
public class GamificationService(
    IGamificationRepository repo,
    XpService xp,
    CurrencyService currency,
    StreakService streak)
{
    // ── XP ───────────────────────────────────────────────────────────────────

    public Task AddXpAsync(int userId, int amount) =>
        xp.AddAsync(userId, amount);

    // ── Монеты ───────────────────────────────────────────────────────────────

    public Task AddCoinsAsync(int userId, int amount) =>
        currency.AddCoinsAsync(userId, amount);

    /// <summary>
    /// Списывает монеты и ключи в транзакции.
    /// Возвращает false и откатывает изменения, если баланс недостаточен.
    /// </summary>
    public Task<bool> DeductCurrencyAsync(int userId, int coins, int keys) =>
        currency.DeductAsync(userId, coins, keys);

    // ── Стрик ────────────────────────────────────────────────────────────────

    /// <summary>Обновляет стрик при любой активности (чтение, тест).</summary>
    public Task UpdateStreakAsync(int userId) =>
        streak.UpdateAsync(userId);

    /// <summary>Проверяет стрик при логине и начисляет ежедневную награду.</summary>
    public Task<StreakCheckResult> CheckStreakOnLoginAsync(int userId) =>
        streak.CheckOnLoginAsync(userId);

    // ── Прямой доступ к профилю (для AuthService и ProfileService) ───────────

    public Task<GamificationProfile?> GetAsync(int userId) =>
        repo.GetAsync(userId);

    public Task SaveAsync(GamificationProfile profile) =>
        repo.SaveAsync(profile);

    // ── Legacy-методы (оставлены для обратной совместимости) ─────────────────

    /// <summary>
    /// Разблокирует стандарт C++ в legacy-поле UnlockedContentStds.
    /// Используется только для старых данных — новые покупки идут через UserPurchases.
    /// </summary>
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

    public async Task<HashSet<string>> GetUnlockedSlotsAsync(int userId)
    {
        var profile = await repo.GetAsync(userId);
        if (profile is null) return [];
        return (profile.UnlockedSlots ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
    }
}
