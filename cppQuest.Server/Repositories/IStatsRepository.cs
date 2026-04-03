using cppQuest.Server.Models;

namespace cppQuest.Server.Repositories;

public interface IStatsRepository
{
    // Reading
    Task<ParagraphReadingStats?> GetReadingStatsAsync(int userId, string paragraphId);
    Task SaveReadingStatsAsync(ParagraphReadingStats stats);
    Task<double> GetAvgReadingSecondsAsync(string paragraphId); // среднее по всем пользователям
    Task<Dictionary<string, double>> GetAvgReadingSecondsBulkAsync(IEnumerable<string> paragraphIds);

    // Test stats (aggregate per user+test)
    Task<ParagraphTestStats?> GetTestStatsAsync(int userId, string testId);
    Task SaveTestStatsAsync(ParagraphTestStats stats);

    // All test stats for a paragraph (for progress response)
    Task<List<ParagraphTestStats>> GetTestStatsByParagraphAsync(int userId, string paragraphId);

    // All stats across all paragraphs (for full profile view)
    Task<List<ParagraphReadingStats>> GetAllReadingStatsAsync(int userId);
    Task<List<ParagraphTestStats>> GetAllTestStatsAsync(int userId);

    // Test attempts (history)
    Task AddTestAttemptAsync(TestAttempt attempt);
    Task<TestAttempt?> GetLastAttemptAsync(int userId, string testId);

    // Возвращает множество ID вопросов, на которые пользователь хоть раз ответил правильно
    Task<HashSet<int>> GetEverCorrectQuestionIdsAsync(int userId, string testId);

    // Возвращает множество ID вопросов, которые пользователь хоть раз видел (правильно или нет)
    Task<HashSet<int>> GetSeenQuestionIdsAsync(int userId, string testId);

    // Возвращает ID вопросов из последней попытки, на которые ответили неправильно
    Task<List<int>> GetLastWrongQuestionIdsAsync(int userId, string testId);

    // Activity log
    Task AddActivityLogAsync(ActivityLog log);
}
