namespace cppQuest.Server.Models;

/// <summary>
/// Статистика по попыткам теста для пользователя и параграфа.
/// Хранит агрегированную информацию (количество попыток, лучший балл и т.п.).
/// PK: (UserId, TestId)
/// </summary>
public class ParagraphTestStats
{
    public int UserId { get; set; }

    /// <summary>Уникальный идентификатор теста (например "cpp-basics-final").</summary>
    public string TestId { get; set; } = "";

    /// <summary>Параграф, к которому относится тест.</summary>
    public string ParagraphId { get; set; } = "";

    /// <summary>Название теста (копируется из JSON при первой попытке).</summary>
    public string TestTitle { get; set; } = "";

    /// <summary>Сколько раз пытались пройти тест.</summary>
    public int AttemptsCount { get; set; }

    /// <summary>Лучший процент (0–100).</summary>
    public int BestScore { get; set; }

    /// <summary>Сумма процентов по всем попыткам; среднее = TotalScoreSum / AttemptsCount.</summary>
    public int TotalScoreSum { get; set; }

    /// <summary>Процент в последней попытке.</summary>
    public int LastScore { get; set; }

    /// <summary>Лучший статус: 0=none, 1=passed, 2=bronze, 3=silver, 4=gold.</summary>
    public int BestStatus { get; set; }

    /// <summary>
    /// Битовая маска выданных наград за освоение банка вопросов.
    /// Бит 0 = 70%, бит 1 = 80%, бит 2 = 90%, бит 3 = 100% банка решено верно.
    /// Позволяет выдавать XP за каждый порог ровно один раз, даже если банк вырастет.
    /// </summary>
    public int BankMasteryXpAwarded { get; set; }

    /// <summary>Время последней попытки.</summary>
    public DateTime LastAttemptAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
