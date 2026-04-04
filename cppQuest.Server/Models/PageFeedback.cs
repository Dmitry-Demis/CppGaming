namespace cppQuest.Server.Models;

/// <summary>
/// Отзыв о странице от пользователя (рейтинг + комментарий).
/// </summary>
public class PageFeedback
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Идентификатор страницы (без пути), например "signed-unsigned".</summary>
    public string PageId { get; set; } = "";

    /// <summary>Оценка страницы в диапазоне 1–10.</summary>
    public int Rating { get; set; }

    /// <summary>Опциональный комментарий пользователя (до 1024 символов).</summary>
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
