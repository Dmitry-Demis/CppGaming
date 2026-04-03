namespace cppQuest.Server.Models;

public class ActivityLog
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // "test_complete" | "read_paragraph" | "code_run" | "login"
    public string ActionType { get; set; } = "";
    public string ParagraphId { get; set; } = "";
    public string TestId { get; set; } = "";

    public int XpEarned { get; set; }
    public int CoinsEarned { get; set; }
    public int TimeSpent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
