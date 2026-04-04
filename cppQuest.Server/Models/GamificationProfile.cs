namespace cppQuest.Server.Models;

/// <summary>
/// Профиль геймификации пользователя: XP, уровень, стрик, валюты и прочее.
/// PK: UserId
/// </summary>
public class GamificationProfile
{
    /// <summary>FK на пользователя.</summary>
    public int UserId { get; set; }

    /// <summary>Текущее XP пользователя.</summary>
    public int Xp { get; set; }

    /// <summary>Текущий уровень (1+).</summary>
    public int Level { get; set; } = 1;

    /// <summary>Виртуальная валюта — монеты.</summary>
    public int Coins { get; set; }

    /// <summary>Текущая цепочка дней активности (streak).</summary>
    public int CurrentStreak { get; set; }

    /// <summary>Максимальный достигнутый стрик.</summary>
    public int MaxStreak { get; set; }

    /// <summary>Дата последней активности.</summary>
    public DateTime LastActivityDate { get; set; } = DateTime.UtcNow;

    // Streak freeze system
    /// <summary>Сколько имеется "заморозок" стрика.</summary>
    public int FreezeCount { get; set; } = 0;

    /// <summary>Количество снежинок (специальная валюта/ресурс).</summary>
    public int SnowflakeCount { get; set; } = 0;

    /// <summary>Всего дней стрика за всё время.</summary>
    public int TotalStreakDays { get; set; } = 0;

    // Валюты
    public int Keys { get; set; } = 0;

    /// <summary>Суммарное значение прокрутки по всем страницам (в пикселях).</summary>
    public long TotalScrollPixels { get; set; } = 0;

    // Legacy строки — оставлены для совместимости
    public string UnlockedContentStds { get; set; } = "";
    public string UnlockedSlots { get; set; } = "";

    // Navigation
    public User User { get; set; } = null!;
}
