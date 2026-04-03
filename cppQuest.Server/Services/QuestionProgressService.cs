using cppQuest.Server.Models;
using cppQuest.Server.Repositories;

namespace cppQuest.Server.Services;

public class QuestionProgressService(IQuestionProgressRepository repo)
{
    // Интервалы SR: 1→2→4→8→16→30 дней
    private static int NextInterval(int currentInterval, bool wasCorrect) =>
        wasCorrect
            ? Math.Min(currentInterval * 2, 30)
            : 1;

    /// <summary>
    /// Возвращает pick ID вопросов по алгоритму приоритетов:
    /// 1. Неправильные из последней попытки (но не более 50% от pick)
    /// 2. Новые (никогда не видели)
    /// 3. Правильные, самые давние (по LastSeenAt)
    /// </summary>
    public async Task<List<int>> PickQuestionIdsAsync(
        int userId, string testId, List<int> allQuestionIds, int pick)
    {
        var progressMap = (await repo.GetByTestAsync(userId, testId))
            .ToDictionary(q => q.QuestionId);

        var rng = new Random();

        // Неправильные из последней попытки (IsCorrect=false)
        var wrong = allQuestionIds
            .Where(id => progressMap.TryGetValue(id, out var p) && !p.IsCorrect)
            .OrderBy(_ => rng.Next())
            .ToList();

        // Новые — никогда не видели
        var unseen = allQuestionIds
            .Where(id => !progressMap.ContainsKey(id))
            .OrderBy(_ => rng.Next())
            .ToList();

        // Видели и ответили правильно — самые давние первыми
        var seenCorrect = allQuestionIds
            .Where(id => progressMap.TryGetValue(id, out var p) && p.IsCorrect)
            .OrderBy(id => progressMap[id].LastSeenAt ?? DateTime.MinValue)
            .ToList();

        var result = new List<int>();

        // Ограничиваем неправильные 50% от pick, чтобы всегда были новые/старые вопросы
        int wrongCap = (int)Math.Ceiling(pick * 0.5);
        var wrongToAdd = wrong.Take(wrongCap).ToList();
        result.AddRange(wrongToAdd);

        // Добираем сначала новыми, потом самыми давними правильными
        foreach (var id in unseen.Concat(seenCorrect))
        {
            if (result.Count >= pick) break;
            if (!result.Contains(id)) result.Add(id);
        }

        // Если всё ещё не хватает (банк маленький) — добираем оставшимися неправильными
        foreach (var id in wrong.Skip(wrongCap))
        {
            if (result.Count >= pick) break;
            result.Add(id);
        }

        // Перемешиваем финальный список
        for (int i = result.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }

    /// <summary>
    /// Обновляет прогресс по каждому вопросу после завершения теста.
    /// answers: QuestionId → isCorrect
    /// </summary>
    public async Task UpdateProgressAsync(
        int userId, string testId, string paragraphId,
        Dictionary<int, bool> answers)
    {
        var today = DateTime.UtcNow.Date;
        var progressMap = (await repo.GetByTestAsync(userId, testId))
            .ToDictionary(q => q.QuestionId);

        var updates = new List<QuestionProgress>();

        foreach (var (questionId, isCorrect) in answers)
        {
            progressMap.TryGetValue(questionId, out var existing);

            int prevStreak   = existing?.CorrectStreak ?? 0;
            int prevInterval = existing?.SrInterval ?? 1;

            int newStreak   = isCorrect ? prevStreak + 1 : 0;
            int newInterval = isCorrect
                ? NextInterval(prevInterval, true)
                : 1;

            updates.Add(new QuestionProgress
            {
                UserId       = userId,
                TestId       = testId,
                QuestionId   = questionId,
                ParagraphId  = paragraphId,
                IsCorrect    = isCorrect,
                CorrectStreak = newStreak,
                SrInterval   = newInterval,
                LastSeenAt   = DateTime.UtcNow,
                NextDueAt    = today.AddDays(newInterval)
            });
        }

        await repo.UpsertBatchAsync(updates);
    }

    /// <summary>
    /// Возвращает параграфы, у которых есть вопросы к повтору сегодня.
    /// Группирует по paragraphId, считает кол-во просроченных вопросов.
    /// </summary>
    public async Task<List<ReviewDueInfo>> GetDueParagraphsAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;
        var all = await repo.GetAllAsync(userId);

        return all
            .Where(q => q.NextDueAt.HasValue && q.NextDueAt.Value.Date <= today)
            .GroupBy(q => q.ParagraphId)
            .Select(g => new ReviewDueInfo(
                g.Key,
                g.Select(q => q.TestId).Distinct().ToList(),
                g.Count(q => !q.IsCorrect),
                g.Count(),
                g.Min(q => q.NextDueAt!.Value)
            ))
            .OrderBy(r => r.OldestDue)
            .ToList();
    }
}

public record ReviewDueInfo(
    string ParagraphId,
    List<string> TestIds,
    int WrongCount,
    int TotalDueCount,
    DateTime OldestDue
);
