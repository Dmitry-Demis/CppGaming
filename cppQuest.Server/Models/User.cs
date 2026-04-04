namespace cppQuest.Server.Models;

public class User
{
    /// <summary>
    /// Внутренний PK пользователя.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Публичный идентификатор, который можно отдавать в клиенте и URL.
    /// Генерируется автоматически при создании пользователя.
    /// </summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    /// <summary>
    /// Номер студента в ISU (внутренний идентификатор учебной системы).
    /// Обычно уникален в пределах института и используется для аутентификации/поиска.
    /// </summary>
    public string IsuNumber { get; set; } = "";

    /// <summary>
    /// Хэш пароля. Алгоритм хеширования задаётся в сервисе аутентификации.
    /// </summary>
    public string PasswordHash { get; set; } = "";

    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;

    /// <summary>
    /// Дата и время регистрации (UTC).
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата последнего входа в систему (UTC), если есть.
    /// </summary>
    public DateTime? LastLoginDate { get; set; }

    // Navigation properties — используются EF для загрузки связанных сущностей.
    public GamificationProfile? GamificationProfile { get; set; }

    /// <summary>Статистика чтения параграфов для пользователя.</summary>
    public List<ParagraphReadingStats> ReadingStats { get; set; } = [];

    /// <summary>Статистика по тестам (аггрегаты на уровне параграфа/теста).</summary>
    public List<ParagraphTestStats> TestStats { get; set; } = [];

    /// <summary>История попыток прохождения тестов пользователем.</summary>
    public List<TestAttempt> TestAttempts { get; set; } = [];

    /// <summary>Логи активности пользователя.</summary>
    public List<ActivityLog> ActivityLogs { get; set; } = [];

    /// <summary>Прогресс по отдельным вопросам (используется для повторений).</summary>
    public List<QuestionProgress> QuestionProgress { get; set; } = [];

    /// <summary>Список разблокированных достижений пользователя.</summary>
    public List<UserAchievement> Achievements { get; set; } = [];
}
