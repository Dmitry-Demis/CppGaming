namespace cppQuest.Server.Models;

/// <summary>
/// Лог действий пользователя: служит для аналитики и отката
/// (например, начисление XP, выполнения теста, запуск кода).
/// </summary>
public class ActivityLog
{
    /// <summary>PK</summary>
    public int Id { get; set; }

    /// <summary>FK на таблицу пользователей.</summary>
    public int UserId { get; set; }

    /// <summary>
    /// Тип действия. Примеры: "test_complete", "read_paragraph", "code_run", "login".
    /// </summary>
    public string ActionType { get; set; } = "";

    /// <summary>Опционально — к какому параграфу относится событие.</summary>
    public string ParagraphId { get; set; } = "";

    /// <summary>Опционально — к какому тесту относится событие.</summary>
    public string TestId { get; set; } = "";

    /// <summary>Сколько XP добавлено пользователю в рамках этого события.</summary>
    public int XpEarned { get; set; }

    /// <summary>Сколько монет начислено.</summary>
    public int CoinsEarned { get; set; }

    /// <summary>Затраченное время (в секундах) для события, если применимо.</summary>
    public int TimeSpent { get; set; }

    /// <summary>Время создания записи.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
