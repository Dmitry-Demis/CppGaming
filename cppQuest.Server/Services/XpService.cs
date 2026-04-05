using cppQuest.Server.Models;
using cppQuest.Server.Repositories;

namespace cppQuest.Server.Services;

/// <summary>
/// Отвечает исключительно за XP и уровень пользователя.
///
/// Алгоритм уровня:
///   level = floor(xp / 500) + 1
///   Т.е. каждые 500 XP — новый уровень, начиная с 1.
/// </summary>
public class XpService(IGamificationRepository repo)
{
    /// <summary>XP, необходимое для перехода на следующий уровень.</summary>
    public const int XpPerLevel = 500;

    /// <summary>Вычисляет уровень по суммарному XP.</summary>
    public static int CalcLevel(int xp) => xp / XpPerLevel + 1;

    /// <summary>Нижняя граница XP для заданного уровня.</summary>
    public static int MinXpForLevel(int level) => (level - 1) * XpPerLevel;

    /// <summary>
    /// Приводит XP и Level в консистентное состояние после ручного изменения в БД.
    ///
    /// Правила:
    ///   1. XP изменён, Level не соответствует XP
    ///      → Level пересчитывается из XP (XP — источник истины).
    ///   2. Level изменён, XP не соответствует новому уровню (XP ниже нижней границы)
    ///      → XP выставляется в нижнюю границу уровня: (Level-1) * XpPerLevel.
    ///   3. Оба изменены и противоречат друг другу
    ///      → XP приоритетнее: Level пересчитывается из XP.
    ///
    /// Метод мутирует профиль и возвращает true, если были исправления
    /// (чтобы вызывающий код мог сохранить изменения в БД).
    /// </summary>
    public static bool Normalize(GamificationProfile profile)
    {
        int expectedLevel = CalcLevel(profile.Xp);
        int minXp         = MinXpForLevel(profile.Level);

        // XP соответствует уровню — всё консистентно
        if (profile.Level == expectedLevel) return false;

        if (profile.Xp >= minXp)
        {
            // XP достаточен для текущего Level, но Level не совпадает с CalcLevel(Xp)
            // → XP был изменён вручную, пересчитываем Level из XP
            profile.Level = expectedLevel;
        }
        else
        {
            // XP меньше нижней границы текущего Level
            // → Level был изменён вручную, выставляем XP в нижнюю границу
            profile.Xp = minXp;
        }

        return true;
    }

    /// <summary>
    /// Начисляет XP и пересчитывает уровень.
    /// Не сохраняет профиль — вызывающий код должен вызвать repo.SaveAsync сам
    /// (чтобы несколько изменений можно было сохранить одной транзакцией).
    /// </summary>
    public static void Apply(GamificationProfile profile, int amount)
    {
        if (amount <= 0) return;
        profile.Xp   += amount;
        profile.Level = CalcLevel(profile.Xp);
    }

    /// <summary>Начисляет XP и сохраняет профиль.</summary>
    public async Task AddAsync(int userId, int amount)
    {
        if (amount <= 0) return;
        var profile = await repo.GetAsync(userId);
        if (profile is null) return;
        Apply(profile, amount);
        await repo.SaveAsync(profile);
    }
}
