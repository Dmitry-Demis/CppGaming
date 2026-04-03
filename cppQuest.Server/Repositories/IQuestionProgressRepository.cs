using cppQuest.Server.Models;

namespace cppQuest.Server.Repositories;

public interface IQuestionProgressRepository
{
    /// <summary>Все записи прогресса пользователя по тесту</summary>
    Task<List<QuestionProgress>> GetByTestAsync(int userId, string testId);

    /// <summary>Все записи прогресса пользователя по параграфу (все тесты)</summary>
    Task<List<QuestionProgress>> GetByParagraphAsync(int userId, string paragraphId);

    /// <summary>Все записи прогресса пользователя (для review/due)</summary>
    Task<List<QuestionProgress>> GetAllAsync(int userId);

    /// <summary>Upsert одной записи</summary>
    Task UpsertAsync(QuestionProgress progress);

    /// <summary>Batch upsert нескольких записей</summary>
    Task UpsertBatchAsync(IEnumerable<QuestionProgress> items);
}
