namespace cppQuest.Server.Models;

public class GamificationProfile
{
    public int UserId { get; set; }
    public int Xp { get; set; }
    public int Level { get; set; } = 1;
    public int Coins { get; set; }
    public int CurrentStreak { get; set; }
    public int MaxStreak { get; set; }
    public DateTime LastActivityDate { get; set; } = DateTime.UtcNow;

    // Streak freeze system
    public int FreezeCount { get; set; } = 0;
    public int SnowflakeCount { get; set; } = 0;
    public int TotalStreakDays { get; set; } = 0;

    // Валюты
    public int Keys { get; set; } = 0;

    // Суммарная прокрутка по всем страницам (пиксели)
    public long TotalScrollPixels { get; set; } = 0;

    // Legacy строки — больше не используются, оставлены для совместимости
    public string UnlockedContentStds { get; set; } = "";
    public string UnlockedSlots { get; set; } = "";

    // Navigation
    public User User { get; set; } = null!;
}
