using Microsoft.EntityFrameworkCore;
using cppQuest.Server.Models;

namespace cppQuest.Server.Repositories;

public class StatsRepository(AppDbContext db) : IStatsRepository
{
    public async Task<double> GetAvgReadingSecondsAsync(string paragraphId) =>
        await db.ParagraphReadingStats
            .Where(r => r.ParagraphId == paragraphId && r.TotalReadingSeconds > 0)
            .AverageAsync(r => (double?)r.TotalReadingSeconds) ?? 0;

    public async Task<Dictionary<string, double>> GetAvgReadingSecondsBulkAsync(IEnumerable<string> paragraphIds)
    {
        var ids = paragraphIds.ToList();
        var rows = await db.ParagraphReadingStats
            .Where(r => ids.Contains(r.ParagraphId) && r.TotalReadingSeconds > 0)
            .GroupBy(r => r.ParagraphId)
            .Select(g => new { ParagraphId = g.Key, Avg = g.Average(r => (double)r.TotalReadingSeconds) })
            .ToListAsync();
        return rows.ToDictionary(r => r.ParagraphId, r => r.Avg);
    }

    public async Task<ParagraphReadingStats?> GetReadingStatsAsync(int userId, string paragraphId) =>
        await db.ParagraphReadingStats
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ParagraphId == paragraphId);

    public async Task SaveReadingStatsAsync(ParagraphReadingStats stats)
    {
        var existing = await db.ParagraphReadingStats
            .FirstOrDefaultAsync(r => r.UserId == stats.UserId && r.ParagraphId == stats.ParagraphId);
        if (existing is null)
        {
            db.ParagraphReadingStats.Add(stats);
        }
        else
        {
            existing.TotalReadingSeconds   = stats.TotalReadingSeconds;
            existing.ReadingSessionsCount  = stats.ReadingSessionsCount;
            existing.CodeRunsCount         = stats.CodeRunsCount;
            existing.LastReadAt            = stats.LastReadAt;
            existing.SrInterval            = stats.SrInterval;
            existing.SrEaseFactor          = stats.SrEaseFactor;
            existing.SrRepetitions         = stats.SrRepetitions;
            existing.SrNextDue             = stats.SrNextDue;
        }
        await db.SaveChangesAsync();
    }

    public async Task<ParagraphTestStats?> GetTestStatsAsync(int userId, string testId) =>
        await db.ParagraphTestStats
            .FirstOrDefaultAsync(t => t.UserId == userId && t.TestId == testId);

    public async Task SaveTestStatsAsync(ParagraphTestStats stats)
    {
        var existing = await db.ParagraphTestStats
            .FirstOrDefaultAsync(t => t.UserId == stats.UserId && t.TestId == stats.TestId);
        if (existing is null)
        {
            db.ParagraphTestStats.Add(stats);
        }
        else
        {
            existing.AttemptsCount        = stats.AttemptsCount;
            existing.TotalScoreSum        = stats.TotalScoreSum;
            existing.BestScore            = stats.BestScore;
            existing.LastScore            = stats.LastScore;
            existing.LastAttemptAt        = stats.LastAttemptAt;
            existing.ParagraphId          = stats.ParagraphId;
            existing.BestStatus           = stats.BestStatus;
            existing.BankMasteryXpAwarded = stats.BankMasteryXpAwarded;
            if (!string.IsNullOrEmpty(stats.TestTitle) && string.IsNullOrEmpty(existing.TestTitle))
                existing.TestTitle = stats.TestTitle;
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<ParagraphTestStats>> GetTestStatsByParagraphAsync(int userId, string paragraphId) =>
        await db.ParagraphTestStats
            .Where(t => t.UserId == userId && t.ParagraphId == paragraphId)
            .ToListAsync();

    public async Task<List<ParagraphReadingStats>> GetAllReadingStatsAsync(int userId) =>
        await db.ParagraphReadingStats
            .Where(r => r.UserId == userId)
            .ToListAsync();

    public async Task<List<ParagraphTestStats>> GetAllTestStatsAsync(int userId) =>
        await db.ParagraphTestStats
            .Where(t => t.UserId == userId)
            .ToListAsync();

    public async Task AddTestAttemptAsync(TestAttempt attempt)
    {
        db.TestAttempts.Add(attempt);
        await db.SaveChangesAsync();
    }

    public async Task<TestAttempt?> GetLastAttemptAsync(int userId, string testId) =>
        await db.TestAttempts
            .Where(a => a.UserId == userId && a.TestId == testId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<List<int>> GetLastWrongQuestionIdsAsync(int userId, string testId)
    {
        var last = await GetLastAttemptAsync(userId, testId);
        if (last is null || string.IsNullOrEmpty(last.WrongQuestionIds)) return [];
        try { return System.Text.Json.JsonSerializer.Deserialize<List<int>>(last.WrongQuestionIds) ?? []; }
        catch { return []; }
    }

    public async Task<HashSet<int>> GetEverCorrectQuestionIdsAsync(int userId, string testId)
    {
        var allAttempts = await db.TestAttempts
            .Where(a => a.UserId == userId && a.TestId == testId)
            .Select(a => a.CorrectQuestionIds)
            .ToListAsync();

        var result = new HashSet<int>();
        foreach (var json in allAttempts)
        {
            if (string.IsNullOrEmpty(json)) continue;
            try
            {
                var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(json);
                if (ids != null) foreach (var id in ids) result.Add(id);
            }
            catch { /* ignore malformed */ }
        }
        return result;
    }

    public async Task<HashSet<int>> GetSeenQuestionIdsAsync(int userId, string testId)
    {
        var allAttempts = await db.TestAttempts
            .Where(a => a.UserId == userId && a.TestId == testId)
            .Select(a => new { a.CorrectQuestionIds, a.WrongQuestionIds })
            .ToListAsync();

        var result = new HashSet<int>();
        foreach (var attempt in allAttempts)
        {
            foreach (var json in new[] { attempt.CorrectQuestionIds, attempt.WrongQuestionIds })
            {
                if (string.IsNullOrEmpty(json)) continue;
                try
                {
                    var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(json);
                    if (ids != null) foreach (var id in ids) result.Add(id);
                }
                catch { /* ignore malformed */ }
            }
        }
        return result;
    }

    public async Task AddActivityLogAsync(ActivityLog log)
    {
        db.ActivityLogs.Add(log);
        await db.SaveChangesAsync();
    }
}
