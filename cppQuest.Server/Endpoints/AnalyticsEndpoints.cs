using cppQuest.Server.Models;
using cppQuest.Server.Repositories;
using cppQuest.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics");

        // GET /api/analytics/{isuNumber}
        // Returns all analytics data needed for the 7 dashboard charts
        group.MapGet("{isuNumber}", async (
            string isuNumber,
            ProfileService profileService,
            AppDbContext db) =>
        {
            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.NotFound();
            int userId = profile.Id;

            // ── 1. Activity heatmap: daily XP earned last 90 days ──────────
            var since90 = DateTime.UtcNow.Date.AddDays(-89);
            var activityLogs = await db.ActivityLogs
                .Where(l => l.UserId == userId && l.CreatedAt >= since90)
                .ToListAsync();

            var heatmap = activityLogs
                .GroupBy(l => l.CreatedAt.Date)
                .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), xp = g.Sum(l => l.XpEarned), actions = g.Count() })
                .OrderBy(x => x.date)
                .ToList();

            // ── 2. All test attempts (used in sections 2, 5, 9) ───────────
            var allAttemptsList = await db.TestAttempts
                .Where(a => a.UserId == userId)
                .Select(a => new { a.TestId, a.ParagraphId, a.Score, a.CreatedAt })
                .ToListAsync();

            // Test scores over time (last 30 attempts)
            var testTimeline = allAttemptsList
                .OrderByDescending(a => a.CreatedAt)
                .Take(30)
                .OrderBy(a => a.CreatedAt)
                .Select(a => new { date = a.CreatedAt.ToString("yyyy-MM-dd"), score = a.Score, testId = a.TestId, paragraphId = a.ParagraphId })
                .ToList();

            // ── 3. Mastery by paragraph (bar chart) ───────────────────────
            var allTestStats = await db.ParagraphTestStats
                .Where(t => t.UserId == userId && t.AttemptsCount > 0)
                .ToListAsync();

            var masteryByParagraph = allTestStats
                .GroupBy(t => t.ParagraphId)
                .Select(g => new
                {
                    paragraphId = g.Key,
                    mastery = (int)Math.Round(g.Average(t => t.BestScore)),
                    attempts = g.Sum(t => t.AttemptsCount),
                    tests = g.Count()
                })
                .OrderByDescending(x => x.mastery)
                .ToList();

            // ── 4. Reading time by chapter (stacked bar) ──────────────────
            var allReading = await db.ParagraphReadingStats
                .Where(r => r.UserId == userId && r.TotalReadingSeconds > 0)
                .ToListAsync();

            // ── 5. Score distribution (histogram: 0-9, 10-19, ..., 90-100) ─
            var allAttemptScores = allAttemptsList.Select(a => a.Score).ToList();

            var scoreDistribution = Enumerable.Range(0, 10).Select(bucket => new
            {
                label = bucket == 9 ? "90-100" : $"{bucket * 10}-{bucket * 10 + 9}",
                count = allAttemptScores.Count(s => bucket == 9 ? s >= 90 : s >= bucket * 10 && s < (bucket + 1) * 10)
            }).ToList();

            // ── 6. Pass/fail ratio per paragraph ──────────────────────────
            var passFailByParagraph = allTestStats
                .GroupBy(t => t.ParagraphId)
                .Select(g => new
                {
                    paragraphId = g.Key,
                    passed = g.Count(t => t.BestScore >= 70),
                    failed = g.Count(t => t.BestScore < 70)
                })
                .ToList();

            // ── 7. XP earned by action type (pie chart) ───────────────────
            var xpByAction = activityLogs
                .GroupBy(l => l.ActionType)
                .Select(g => new { action = g.Key, xp = g.Sum(l => l.XpEarned), count = g.Count() })
                .OrderByDescending(x => x.xp)
                .ToList();

            // ── 8. Weekly activity (last 12 weeks) ────────────────────────
            var since12w = DateTime.UtcNow.Date.AddDays(-83); // 12 * 7
            var weeklyLogs = await db.ActivityLogs
                .Where(l => l.UserId == userId && l.CreatedAt >= since12w)
                .ToListAsync();

            var weeklyActivity = weeklyLogs
                .GroupBy(l => {
                    var d = l.CreatedAt.Date;
                    // ISO week start (Monday)
                    int diff = (7 + (int)d.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                    return d.AddDays(-diff).ToString("yyyy-MM-dd");
                })
                .Select(g => new
                {
                    weekStart = g.Key,
                    xp = g.Sum(l => l.XpEarned),
                    tests = g.Count(l => l.ActionType == "test_complete"),
                    reads = g.Count(l => l.ActionType == "read_paragraph"),
                    codeRuns = g.Count(l => l.ActionType == "code_run")
                })
                .OrderBy(x => x.weekStart)
                .ToList();

            // ── 9. Improvement trend: first vs best score per test ─────────
            var firstAttempts = allAttemptsList
                .GroupBy(a => a.TestId)
                .Select(g => new { testId = g.Key, firstScore = g.OrderBy(a => a.CreatedAt).First().Score })
                .ToList();

            var improvementData = allTestStats
                .Join(firstAttempts, ts => ts.TestId, fa => fa.testId,
                    (ts, fa) => new
                    {
                        testId = ts.TestId,
                        paragraphId = ts.ParagraphId,
                        firstScore = fa.firstScore,
                        bestScore = ts.BestScore,
                        improvement = ts.BestScore - fa.firstScore
                    })
                .Where(x => x.improvement > 0)
                .OrderByDescending(x => x.improvement)
                .Take(10)
                .ToList();

            return Results.Ok(new
            {
                heatmap,
                testTimeline,
                masteryByParagraph,
                readingStats = allReading.Select(r => new
                {
                    r.ParagraphId,
                    r.TotalReadingSeconds,
                    r.ReadingSessionsCount,
                    r.CodeRunsCount
                }),
                scoreDistribution,
                passFailByParagraph,
                xpByAction,
                weeklyActivity,
                improvementData,
                totalAttempts = allAttemptScores.Count,
                avgScore = allAttemptScores.Count > 0 ? (int)Math.Round(allAttemptScores.Average()) : 0,
                passRate = allAttemptScores.Count > 0
                    ? (int)Math.Round(allAttemptScores.Count(s => s >= 70) * 100.0 / allAttemptScores.Count)
                    : 0
            });
        });
    }
}
