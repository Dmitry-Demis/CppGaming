using cppQuest.Server.Models;
using cppQuest.Server.Repositories;
using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Services;

/// <summary>
/// Отвечает за монеты и ключи пользователя.
/// Все операции списания выполняются в транзакции — если баланс недостаточен,
/// изменения откатываются и метод возвращает false.
/// </summary>
public class CurrencyService(IGamificationRepository repo, AppDbContext db)
{
    /// <summary>Начисляет монеты (без транзакции — только добавление).</summary>
    public static void ApplyCoins(GamificationProfile profile, int amount)
    {
        if (amount > 0) profile.Coins += amount;
    }

    /// <summary>Начисляет монеты и сохраняет.</summary>
    public async Task AddCoinsAsync(int userId, int amount)
    {
        if (amount <= 0) return;
        var profile = await repo.GetAsync(userId);
        if (profile is null) return;
        ApplyCoins(profile, amount);
        await repo.SaveAsync(profile);
    }

    /// <summary>
    /// Списывает монеты и ключи в одной транзакции.
    ///
    /// Алгоритм:
    ///   1. Начинаем транзакцию.
    ///   2. Загружаем профиль с блокировкой (FOR UPDATE через EF).
    ///   3. Проверяем достаточность баланса.
    ///   4. Если недостаточно — откатываем, возвращаем false.
    ///   5. Если достаточно — вычитаем, сохраняем, коммитим, возвращаем true.
    ///
    /// Math.Max(0, ...) — дополнительная защита от отрицательного баланса
    /// на случай race condition между проверкой и списанием.
    /// </summary>
    /// <returns>true — списание прошло; false — недостаточно средств.</returns>
    public async Task<bool> DeductAsync(int userId, int coins, int keys)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var profile = await repo.GetAsync(userId);
            if (profile is null) { await tx.RollbackAsync(); return false; }

            if (coins > 0 && profile.Coins < coins) { await tx.RollbackAsync(); return false; }
            if (keys  > 0 && profile.Keys  < keys)  { await tx.RollbackAsync(); return false; }

            if (coins > 0) profile.Coins = Math.Max(0, profile.Coins - coins);
            if (keys  > 0) profile.Keys  = Math.Max(0, profile.Keys  - keys);

            await repo.SaveAsync(profile);
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
