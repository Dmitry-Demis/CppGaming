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

    // п.8 — номера вопросов с ошибками, JSON: "[3,7,12]"
    public string WrongQuestionIds { get; set; } = "[]";

    // номера вопросов, на которые ответили правильно в этой попытке
    public string CorrectQuestionIds { get; set; } = "[]";

    public int TimeSpent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
