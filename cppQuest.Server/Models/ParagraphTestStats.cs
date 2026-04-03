namespace cppQuest.Server.Models;

public class ParagraphTestStats
{
    public int UserId { get; set; }
    // TestId — уникальный идентификатор теста (например "cpp-basics-final")
    public string TestId { get; set; } = "";
    // ParagraphId — к какому параграфу относится тест
    public string ParagraphId { get; set; } = "";
    // Название теста (из JSON, сохраняется при первой попытке)
    public string TestTitle { get; set; } = "";

    // п.4 — сколько раз решали тест
    public int AttemptsCount { get; set; }
    // п.5 — лучший процент
    public int BestScore { get; set; }
    // для п.6 — сумма всех процентов; среднее = TotalScoreSum / AttemptsCount
    public int TotalScoreSum { get; set; }
    // п.7 — текущий (последний) процент
    public int LastScore { get; set; }

    // Лучший достигнутый статус: 0=none, 1=passed, 2=bronze, 3=silver, 4=gold
    public int BestStatus { get; set; }

    public DateTime LastAttemptAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
