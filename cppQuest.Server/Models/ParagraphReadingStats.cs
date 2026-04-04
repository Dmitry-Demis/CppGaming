namespace cppQuest.Server.Models;

/// <summary>
/// Статистика чтения параграфа пользователем (время, прокрутки, SR-поля).
/// PK: (UserId, ParagraphId)
/// </summary>
public class ParagraphReadingStats
{
    public int UserId { get; set; }
    public string ParagraphId { get; set; } = "";

    /// <summary>Суммарное время чтения в секундах.</summary>
    public int TotalReadingSeconds { get; set; }

    /// <summary>Количество сессий чтения.</summary>
    public int ReadingSessionsCount { get; set; }

    /// <summary>Сколько раз запускали примеры/код на странице.</summary>
    public int CodeRunsCount { get; set; }

    /// <summary>Суммарно прокручено пикселей на странице.</summary>
    public long ScrollPixels { get; set; }

    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

    // SM-2 spaced repetition: interval in days until next review
    public int SrInterval { get; set; } = 1;
    public float SrEaseFactor { get; set; } = 2.5f;
    public int SrRepetitions { get; set; } = 0;
    public DateTime? SrNextDue { get; set; }

    public User User { get; set; } = null!;
}
