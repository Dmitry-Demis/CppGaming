namespace cppQuest.Server.Models;

public class User
{
    public int Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string IsuNumber { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginDate { get; set; }

    // Navigation
    public GamificationProfile? GamificationProfile { get; set; }
    public List<ParagraphReadingStats> ReadingStats { get; set; } = [];
    public List<ParagraphTestStats> TestStats { get; set; } = [];
    public List<TestAttempt> TestAttempts { get; set; } = [];
    public List<ActivityLog> ActivityLogs { get; set; } = [];
    public List<QuestionProgress> QuestionProgress { get; set; } = [];
    public List<UserAchievement> Achievements { get; set; } = [];
}
