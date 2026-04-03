namespace cppQuest.Server.Models;

/// <summary>
/// Прогресс пользователя по конкретному вопросу теста.
/// PK: (UserId, TestId, QuestionId)
/// </summary>
public class QuestionProgress
{
    public int UserId { get; set; }
    public string TestId { get; set; } = "";
    public int QuestionId { get; set; }

    // Для удобства — к какому параграфу относится тест
    public string ParagraphId { get; set; } = "";

    // Последний ответ был верным?
    public bool IsCorrect { get; set; }

    // Сколько раз подряд ответили верно
    public int CorrectStreak { get; set; }

    // Текущий SR-интервал в днях (1→2→4→8→16→30)
    public int SrInterval { get; set; } = 1;

    // Когда последний раз видели этот вопрос
    public DateTime? LastSeenAt { get; set; }

    // Когда показать снова (null = ещё не видели)
    public DateTime? NextDueAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
