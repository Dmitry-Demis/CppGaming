namespace cppQuest.Server.Models;

public class TestAttempt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ParagraphId { get; set; } = "";
    public string TestId { get; set; } = "";

    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Номера вопросов, на которых пользователь ошибся в этой попытке.
    /// Хранится как JSON-массив, например: "[3,7,12]".
    /// </summary>
    public string WrongQuestionIds { get; set; } = "[]";

    /// <summary>
    /// Номера вопросов, на которые ответили правильно в этой попытке (JSON-массив).
    /// </summary>
    public string CorrectQuestionIds { get; set; } = "[]";

    /// <summary>Затраченное время на попытку в секундах.</summary>
    public int TimeSpent { get; set; }

    /// <summary>Время создания попытки.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
