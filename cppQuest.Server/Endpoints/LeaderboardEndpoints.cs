using cppQuest.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Endpoints;

public static class LeaderboardEndpoints
{
    // 96 dpi: 1 inch = 96px, 1 inch = 2.54 cm → 1px = 2.54/96 cm
    private const double PxToCm = 2.54 / 96.0;

    public static void MapLeaderboardEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/leaderboard");

        g.MapGet("xp",      GetXpLeaderboardAsync);
        g.MapGet("coins",   GetCoinsLeaderboardAsync);
        g.MapGet("reading", GetReadingLeaderboardAsync);
        g.MapGet("streak",  GetStreakLeaderboardAsync);
        g.MapGet("tests",   GetTestsLeaderboardAsync);
        g.MapGet("mastery", GetMasteryLeaderboardAsync);
        g.MapGet("scroll",  GetScrollLeaderboardAsync);
    }

    // ── Handlers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Топ-100 по суммарному XP. Уровень вычисляется из XP на стороне сервера.
    /// </summary>
    private static async Task<IResult> GetXpLeaderboardAsync(AppDbContext db)
    {
        var rows = await db.Users
            .Where(u => u.IsActive)
            .Join(db.GamificationProfiles, u => u.Id, gp => gp.UserId,
                (u, gp) => new { u.PublicId, gp.Xp, gp.Level })
            .OrderByDescending(x => x.Xp)
            .Take(100)
            .ToListAsync();

        return Results.Ok(rows.Select((r, i) => new
        {
            rank     = i + 1,
            publicId = r.PublicId,
            xp       = r.Xp,
            level    = r.Level
        }));
    }

    /// <summary>
    /// Топ-100 по суммарно заработанным монетам за всё время (не текущий баланс).
    /// Считается через ActivityLog, а не через GamificationProfile.Coins.
    /// </summary>
    private static async Task<IResult> GetCoinsLeaderboardAsync(AppDbContext db)
    {
        var rows = await db.Users
            .Where(u => u.IsActive)
            .Select(u => new
            {
                u.PublicId,
                // (int?) нужен, чтобы Sum вернул null для пустого набора, а не бросил исключение
                totalCoins = db.ActivityLogs
                    .Where(l => l.UserId == u.Id)
                    .Sum(l => (int?)l.CoinsEarned) ?? 0
            })
            .OrderByDescending(x => x.totalCoins)
            .Take(100)
            .ToListAsync();

        return Results.Ok(rows.Select((r, i) => new
        {
            rank       = i + 1,
            publicId   = r.PublicId,
            totalCoins = r.totalCoins
        }));
    }

    /// <summary>
    /// Топ-100 по количеству прочитанных параграфов. При равенстве — суммарное время чтения.
    /// </summary>
    private static async Task<IResult> GetReadingLeaderboardAsync(AppDbContext db)
    {
        var rows = await db.Users
            .Where(u => u.IsActive)
            .Select(u => new
            {
                u.PublicId,
                paragraphs = db.ParagraphReadingStats
                    .Count(r => r.UserId == u.Id && r.ReadingSessionsCount > 0),
                totalSeconds = db.ParagraphReadingStats
                    .Where(r => r.UserId == u.Id)
                    .Sum(r => (int?)r.TotalReadingSeconds) ?? 0
            })
            .OrderByDescending(x => x.paragraphs)
            .Take(100)
            .ToListAsync();

        return Results.Ok(rows.Select((r, i) => new
        {
            rank          = i + 1,
            publicId      = r.PublicId,
            paragraphs    = r.paragraphs,
            totalSeconds  = r.totalSeconds,
            timeFormatted = FormatDuration(r.totalSeconds)
        }));
    }

    /// <summary>
    /// Топ-100 по максимальному стрику (серии дней подряд).
    /// </summary>
    private static async Task<IResult> GetStreakLeaderboardAsync(AppDbContext db)
    {
        var rows = await db.Users
            .Where(u => u.IsActive)
            .Join(db.GamificationProfiles, u => u.Id, gp => gp.UserId,
                (u, gp) => new { u.PublicId, gp.MaxStreak, gp.CurrentStreak })
            .OrderByDescending(x => x.MaxStreak)
            .Take(100)
            .ToListAsync();

        return Results.Ok(rows.Select((r, i) => new
        {
            rank          = i + 1,
            publicId      = r.PublicId,
            maxStreak     = r.MaxStreak,
            currentStreak = r.CurrentStreak
        }));
    }

    /// <summary>
    /// Топ-100 по количеству попыток тестов. Средний балл — дополнительная метрика.
    /// </summary>
    private static async Task<IResult> GetTestsLeaderboardAsync(AppDbContext db)
    {
        var rows = await db.Users
            .Where(u => u.IsActive)
            .Select(u => new
            {
                u.PublicId,
                testAttempts = db.TestAttempts.Count(a => a.UserId == u.Id),
                avgScore     = db.TestAttempts
                    .Where(a => a.UserId == u.Id)
                    .Average(a => (double?)a.Score) ?? 0.0
            })
            .OrderByDescending(x => x.testAttempts)
            .Take(100)
            .ToListAsync();

        return Results.Ok(rows.Select((r, i) => new
        {
            rank         = i + 1,
            publicId     = r.PublicId,
            testAttempts = r.testAttempts,
            avgScore     = Math.Round(r.avgScore, 1)
        }));
    }

    /// <summary>
    /// Топ-100 по "усвоению": доле тестов, пройденных с результатом ≥70%.
    /// Общее количество тестов считается по файлам в директории <c>tests/</c>.
    /// </summary>
    private static async Task<IResult> GetMasteryLeaderboardAsync(AppDbContext db, IWebHostEnvironment env)
    {
        // Считаем общее кол-во уникальных тестов в файловой системе (знаменатель для процента)
        var testsRoot  = Path.Combine(env.ContentRootPath, "tests");
        int totalTests = Directory.Exists(testsRoot)
            ? Directory.GetFiles(testsRoot, "*.json", SearchOption.AllDirectories).Length
            : 1; // защита от деления на ноль

        var rows = await db.Users
            .Where(u => u.IsActive)
            .Select(u => new
            {
                u.PublicId,
                passedTests    = db.ParagraphTestStats.Count(t => t.UserId == u.Id && t.AttemptsCount > 0 && t.BestScore >= 70),
                attemptedTests = db.ParagraphTestStats.Count(t => t.UserId == u.Id && t.AttemptsCount > 0),
                avgBest        = db.ParagraphTestStats
                    .Where(t => t.UserId == u.Id && t.AttemptsCount > 0)
                    .Average(t => (double?)t.BestScore) ?? 0.0
            })
            .Where(x => x.attemptedTests > 0)
            .OrderByDescending(x => x.passedTests)
            .Take(100)
            .ToListAsync();

        return Results.Ok(rows.Select((r, i) => new
        {
            rank           = i + 1,
            publicId       = r.PublicId,
            passedTests    = r.passedTests,
            attemptedTests = r.attemptedTests,
            totalTests,
            coveragePct    = Math.Round(r.passedTests * 100.0 / totalTests, 1),
            avgBest        = Math.Round(r.avgBest, 1)
        }));
    }

    /// <summary>
    /// Топ-100 по суммарной прокрутке страниц. Пиксели конвертируются в см/м/км для отображения.
    /// </summary>
    private static async Task<IResult> GetScrollLeaderboardAsync(AppDbContext db)
    {
        var rows = await db.Users
            .Where(u => u.IsActive)
            .Join(db.GamificationProfiles, u => u.Id, gp => gp.UserId,
                (u, gp) => new { u.PublicId, gp.TotalScrollPixels })
            .OrderByDescending(x => x.TotalScrollPixels)
            .Take(100)
            .ToListAsync();

        return Results.Ok(rows.Select((r, i) =>
        {
            double cm = r.TotalScrollPixels * PxToCm;
            return new
            {
                rank        = i + 1,
                publicId    = r.PublicId,
                totalPixels = r.TotalScrollPixels,
                display     = FormatDistance(cm)
            };
        }));
    }

    // ── Форматирование ───────────────────────────────────────────────────────

    /// <summary>Форматирует секунды в строку "HH:MM:SS".</summary>
    private static string FormatDuration(int totalSeconds)
    {
        var h = totalSeconds / 3600;
        var m = (totalSeconds % 3600) / 60;
        var s = totalSeconds % 60;
        return $"{h:D2}:{m:D2}:{s:D2}";
    }

    /// <summary>
    /// Форматирует расстояние в человекочитаемый вид: см → м → км.
    /// </summary>
    private static string FormatDistance(double cm) => cm switch
    {
        < 100    => $"{cm:F1} см",
        < 100000 => $"{cm / 100.0:F1} м",
        _        => $"{cm / 100000.0:F2} км"
    };
}
