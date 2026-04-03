namespace cppQuest.Server.Models;

/// <summary>
/// Разблокированное достижение пользователя.
/// PK: (UserId, AchievementId)
/// </summary>
public class UserAchievement
{
    public int UserId { get; set; }
    public string AchievementId { get; set; } = "";
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
