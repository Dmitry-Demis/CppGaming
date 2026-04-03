using Microsoft.EntityFrameworkCore;
using cppQuest.Server.Models;

namespace cppQuest.Server.Repositories;

public class QuestionProgressRepository(AppDbContext db) : IQuestionProgressRepository
{
    public Task<List<QuestionProgress>> GetByTestAsync(int userId, string testId) =>
        db.QuestionProgress
            .Where(q => q.UserId == userId && q.TestId == testId)
            .ToListAsync();

    public Task<List<QuestionProgress>> GetByParagraphAsync(int userId, string paragraphId) =>
        db.QuestionProgress
            .Where(q => q.UserId == userId && q.ParagraphId == paragraphId)
            .ToListAsync();

    public Task<List<QuestionProgress>> GetAllAsync(int userId) =>
        db.QuestionProgress
            .Where(q => q.UserId == userId)
            .ToListAsync();

    public async Task UpsertAsync(QuestionProgress progress)
    {
        var existing = await db.QuestionProgress
            .FirstOrDefaultAsync(q =>
                q.UserId == progress.UserId &&
                q.TestId == progress.TestId &&
                q.QuestionId == progress.QuestionId);

        if (existing is null)
            db.QuestionProgress.Add(progress);
        else
        {
            existing.IsCorrect      = progress.IsCorrect;
            existing.CorrectStreak  = progress.CorrectStreak;
            existing.SrInterval     = progress.SrInterval;
            existing.LastSeenAt     = progress.LastSeenAt;
            existing.NextDueAt      = progress.NextDueAt;
            existing.ParagraphId    = progress.ParagraphId;
        }

        await db.SaveChangesAsync();
    }

    public async Task UpsertBatchAsync(IEnumerable<QuestionProgress> items)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0) return;

        var userId = itemList[0].UserId;
        var testId = itemList[0].TestId;

        var existing = await db.QuestionProgress
            .Where(q => q.UserId == userId && q.TestId == testId)
            .ToListAsync();

        var existingMap = existing.ToDictionary(q => q.QuestionId);

        foreach (var item in itemList)
        {
            if (existingMap.TryGetValue(item.QuestionId, out var ex))
            {
                ex.IsCorrect     = item.IsCorrect;
                ex.CorrectStreak = item.CorrectStreak;
                ex.SrInterval    = item.SrInterval;
                ex.LastSeenAt    = item.LastSeenAt;
                ex.NextDueAt     = item.NextDueAt;
                ex.ParagraphId   = item.ParagraphId;
            }
            else
            {
                db.QuestionProgress.Add(item);
            }
        }

        await db.SaveChangesAsync();
    }
}
