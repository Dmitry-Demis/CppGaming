using cppQuest.Server.Models;
using cppQuest.Server.Repositories;
using cppQuest.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Endpoints;

public static class AnalyticsEndpoints
{

    /// <summary>
    /// Регистрирует маршруты аналитики: `GET /api/analytics/{isuNumber}`.
    /// </summary>
    /// <param name="app">Экземпляр <see cref="IEndpointRouteBuilder"/>, используемый для регистрации маршрутов.</param>
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics");

        // GET /api/analytics/{isuNumber}
        // Делегируем обработку в отдельный метод для читаемости и комментирования.
        group.MapGet("{isuNumber}", (string isuNumber, ProfileService profileService, AppDbContext db)
            => GetAnalyticsForUserAsync(isuNumber, profileService, db));
    }
    /// <summary>
    /// Собирает все аналитические данные для дашборда пользователя.
    /// </summary>
    /// <param name="isuNumber">ISU-номер пользователя (string). Используется для поиска профиля.</param>
    /// <param name="profileService">Сервис профиля — получает внутренний User.Id по ISU.</param>
    /// <param name="db">Экземпляр <see cref="AppDbContext"/> для запросов в БД.</param>
    /// <returns>Возвращает <see cref="IResult"/> с набором аналитических данных или NotFound.</returns>
    private static async Task<IResult> GetAnalyticsForUserAsync(string isuNumber, ProfileService profileService, AppDbContext db)
    {
        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return Results.NotFound();
        int userId = profile.Id;

        // Выполняем отдельные шаги в helper-методах — так легче читать и тестировать.
        var heatmap = await BuildHeatmapAsync(db, userId);
        var attempts = await LoadUserAttemptsAsync(db, userId);
        var testTimeline = BuildTestTimeline(attempts);
        var allTestStats = await LoadUserTestStatsAsync(db, userId);
        var masteryByParagraph = ComputeMasteryByParagraph(allTestStats);
        var allReading = await LoadReadingStatsAsync(db, userId);
        var allAttemptScores = attempts.Select(a => a.Score).ToList();
        var scoreDistribution = ComputeScoreDistribution(allAttemptScores);
        var passFailByParagraph = ComputePassFailByParagraph(allTestStats);
        var xpByAction = ComputeXpByAction(await LoadRecentActivityAsync(db, userId));
        var weeklyActivity = await ComputeWeeklyActivityAsync(db, userId);
        var improvementData = ComputeImprovementData(allTestStats, attempts);

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
    }

    // Helper record for lightweight attempt data
    private record AttemptInfo(string TestId, string ParagraphId, int Score, DateTime CreatedAt);

    /// <summary>
    /// Загружает попытки пользователя из БД в компактную структуру.
    /// </summary>
    /// <param name="db">Экземпляр <see cref="AppDbContext"/>.</param>
    /// <param name="userId">Внутренний идентификатор пользователя.</param>
    /// <returns>Список компактных записей попыток (<see cref="AttemptInfo"/>).</returns>
    private static async Task<List<AttemptInfo>> LoadUserAttemptsAsync(AppDbContext db, int userId)
        => await db.TestAttempts
            .Where(a => a.UserId == userId)
            .Select(a => new AttemptInfo(a.TestId, a.ParagraphId, a.Score, a.CreatedAt))
            .ToListAsync();

    /// <summary>
    /// Загружает статистику тестов (ParagraphTestStats) пользователя.
    /// </summary>
    /// <param name="db">Экземпляр <see cref="AppDbContext"/>.</param>
    /// <param name="userId">Внутренний идентификатор пользователя.</param>
    /// <returns>Список <see cref="ParagraphTestStats"/> для пользователя.</returns>
    private static async Task<List<ParagraphTestStats>> LoadUserTestStatsAsync(AppDbContext db, int userId)
        => await db.ParagraphTestStats
            .Where(t => t.UserId == userId && t.AttemptsCount > 0)
            .ToListAsync();

    /// <summary>
    /// Строит тепловую карту XP по дням за последние 90 дней.
    /// </summary>
    /// <param name="db">Экземпляр <see cref="AppDbContext"/>.</param>
    /// <param name="userId">Внутренний идентификатор пользователя.</param>
    /// <returns>Список объектов { date, xp, actions }.</returns>
    private static async Task<List<object>> BuildHeatmapAsync(AppDbContext db, int userId)
    {
        var since90 = DateTime.UtcNow.Date.AddDays(-89);
        var activityLogs = await db.ActivityLogs
            .Where(l => l.UserId == userId && l.CreatedAt >= since90)
            .ToListAsync();

        return activityLogs
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), xp = g.Sum(l => l.XpEarned), actions = g.Count() })
            .OrderBy(x => x.date)
            .ToList<object>();
    }

    /// <summary>
    /// Читает статистику чтения пользователя.
    /// </summary>
    /// <param name="db">Экземпляр <see cref="AppDbContext"/>.</param>
    /// <param name="userId">Внутренний идентификатор пользователя.</param>
    /// <returns>Список <see cref="ParagraphReadingStats"/>.</returns>
    private static async Task<List<ParagraphReadingStats>> LoadReadingStatsAsync(AppDbContext db, int userId)
        => await db.ParagraphReadingStats
            .Where(r => r.UserId == userId && r.TotalReadingSeconds > 0)
            .ToListAsync();

    /// <summary>
    /// Строит хронологический ряд последних 30 попыток (для графика).
    /// </summary>
    /// <param name="attempts">Список попыток (<see cref="AttemptInfo"/>).</param>
    /// <returns>Список объектов { date, score, testId, paragraphId }.</returns>
    private static List<object> BuildTestTimeline(List<AttemptInfo> attempts)
        => attempts
            .OrderByDescending(a => a.CreatedAt)
            .Take(30)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new { date = a.CreatedAt.ToString("yyyy-MM-dd"), score = a.Score, testId = a.TestId, paragraphId = a.ParagraphId })
            .ToList<object>();

    /// <summary>
    /// Вычисляет mastery по параграфам (средний лучший балл, суммарные попытки и количество тестов).
    /// </summary>
    /// <param name="stats">Список <see cref="ParagraphTestStats"/> пользователя.</param>
    /// <returns>Список агрегатов mastery по параграфам.</returns>
    private static List<object> ComputeMasteryByParagraph(List<ParagraphTestStats> stats)
        => stats
            .GroupBy(t => t.ParagraphId)
            .Select(g => new
            {
                paragraphId = g.Key,
                mastery = (int)Math.Round(g.Average(t => t.BestScore)),
                attempts = g.Sum(t => t.AttemptsCount),
                tests = g.Count()
            })
            .OrderByDescending(x => x.mastery)
            .ToList<object>();

    /// <summary>
    /// Считает распределение результатов в 10 корзин (0-9,10-19,...,90-100).
    /// </summary>
    /// <param name="scores">Список баллов пользователей.</param>
    /// <returns>Список корзин с меткой и количеством.</returns>
    private static List<object> ComputeScoreDistribution(List<int> scores)
    {
        // bucket==9 включает 90–100
        return Enumerable.Range(0, 10).Select(bucket => new
        {
            label = bucket == 9 ? "90-100" : $"{bucket * 10}-{bucket * 10 + 9}",
            count = scores.Count(s => bucket == 9 ? s >= 90 : s >= bucket * 10 && s < (bucket + 1) * 10)
        }).ToList<object>();
    }

    /// <summary>
    /// Вычисляет pass/fail по параграфам (порог 70%).
    /// </summary>
    /// <param name="stats">Список <see cref="ParagraphTestStats"/>.</param>
    /// <returns>Список объектов { paragraphId, passed, failed }.</returns>
    private static List<object> ComputePassFailByParagraph(List<ParagraphTestStats> stats)
        => stats
            .GroupBy(t => t.ParagraphId)
            .Select(g => new
            {
                paragraphId = g.Key,
                passed = g.Count(t => t.BestScore >= 70),
                failed = g.Count(t => t.BestScore < 70)
            })
            .ToList<object>();

    /// <summary>
    /// Группирует XP по типам действий.
    /// </summary>
    /// <param name="logs">Список <see cref="ActivityLog"/> для агрегации.</param>
    /// <returns>Список объектов { action, xp, count }.</returns>
    private static List<object> ComputeXpByAction(List<ActivityLog> logs)
        => logs
            .GroupBy(l => l.ActionType)
            .Select(g => new { action = g.Key, xp = g.Sum(l => l.XpEarned), count = g.Count() })
            .OrderByDescending(x => x.xp)
            .ToList<object>();

    /// <summary>
    /// Загружает недавние логи активности пользователя (используется для XP по типам).
    /// </summary>
    /// <param name="db">Экземпляр <see cref="AppDbContext"/>.</param>
    /// <param name="userId">Внутренний идентификатор пользователя.</param>
    /// <returns>Список <see cref="ActivityLog"/> за последние 90 дней.</returns>
    private static async Task<List<ActivityLog>> LoadRecentActivityAsync(AppDbContext db, int userId)
    {
        var since90 = DateTime.UtcNow.Date.AddDays(-89);
        return await db.ActivityLogs
            .Where(l => l.UserId == userId && l.CreatedAt >= since90)
            .ToListAsync();
    }

    /// <summary>
    /// Вычисляет недельную активность за последние 12 недель, группируя по ISO-неделям (понедельник).
    /// </summary>
    /// <param name="db">Экземпляр <see cref="AppDbContext"/>.</param>
    /// <param name="userId">Внутренний идентификатор пользователя.</param>
    /// <returns>Список агрегатов по неделям.</returns>
    private static async Task<List<object>> ComputeWeeklyActivityAsync(AppDbContext db, int userId)
    {
        var since12w = DateTime.UtcNow.Date.AddDays(-83); // ~12 weeks
        var weeklyLogs = await db.ActivityLogs
            .Where(l => l.UserId == userId && l.CreatedAt >= since12w)
            .ToListAsync();

        // ISO week start = Monday. diff вычисляет смещение до понедельника.
        return weeklyLogs
            .GroupBy(l =>
            {
                var d = l.CreatedAt.Date;
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
            .ToList<object>();
    }

    /// <summary>
    /// Вычисляет улучшение (best - first) и возвращает топ N улучшений.
    /// </summary>
    /// <param name="stats">Список <see cref="ParagraphTestStats"/> с лучшими результатами.</param>
    /// <param name="attempts">Список первых/всех попыток (<see cref="AttemptInfo"/>).</param>
    /// <returns>Список лучших улучшений по тестам.</returns>
    private static List<object> ComputeImprovementData(List<ParagraphTestStats> stats, List<AttemptInfo> attempts)
    {
        var firstAttempts = attempts
            .GroupBy(a => a.TestId)
            .Select(g => new { testId = g.Key, firstScore = g.OrderBy(a => a.CreatedAt).First().Score })
            .ToList();

        return stats
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
            .ToList<object>();
    }
}
