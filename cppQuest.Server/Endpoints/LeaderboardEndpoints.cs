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

        // ── Уровень и опыт ────────────────────────────────────────────────
        g.MapGet("xp", async (AppDbContext db) =>
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
        });

        // ── Заработано монет за всё время ────────────────────────────────
        g.MapGet("coins", async (AppDbContext db) =>
        {
            var rows = await db.Users
                .Where(u => u.IsActive)
                .Select(u => new
                {
                    u.PublicId,
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
        });

        // ── Прочитано параграфов + суммарное время ────────────────────────
        g.MapGet("reading", async (AppDbContext db) =>
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
                rank         = i + 1,
                publicId     = r.PublicId,
                paragraphs   = r.paragraphs,
                totalSeconds = r.totalSeconds,
                timeFormatted = FormatDuration(r.totalSeconds)
            }));
        });

        // ── Серия дней (стрик) ────────────────────────────────────────────
        g.MapGet("streak", async (AppDbContext db) =>
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
        });

        // ── Прохождений тестов ────────────────────────────────────────────
        g.MapGet("tests", async (AppDbContext db) =>
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
        });

        // ── Усвоение: % пройденных тестов от всех тестов в системе ────────
        g.MapGet("mastery", async (AppDbContext db, IWebHostEnvironment env) =>
        {
            // Считаем общее кол-во уникальных тестов в файловой системе
            var testsRoot = Path.Combine(env.ContentRootPath, "tests");
            int totalTests = Directory.Exists(testsRoot)
                ? Directory.GetFiles(testsRoot, "*.json", SearchOption.AllDirectories).Length
                : 1;

            var rows = await db.Users
                .Where(u => u.IsActive)
                .Select(u => new
                {
                    u.PublicId,
                    passedTests = db.ParagraphTestStats
                        .Count(t => t.UserId == u.Id && t.AttemptsCount > 0 && t.BestScore >= 70),
                    attemptedTests = db.ParagraphTestStats
                        .Count(t => t.UserId == u.Id && t.AttemptsCount > 0),
                    avgBest = db.ParagraphTestStats
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
        });

        // ── Прокрутка ─────────────────────────────────────────────────────
        g.MapGet("scroll", async (AppDbContext db) =>
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
        });
    }

    // "00:01:23" из секунд
    private static string FormatDuration(int totalSeconds)
    {
        var h = totalSeconds / 3600;
        var m = (totalSeconds % 3600) / 60;
        var s = totalSeconds % 60;
        return $"{h:D2}:{m:D2}:{s:D2}";
    }

    // Умный формат расстояния: см → м → км
    private static string FormatDistance(double cm)
    {
        if (cm < 100) return $"{cm:F1} см";
        double m = cm / 100.0;
        if (m < 1000) return $"{m:F1} м";
        double km = m / 1000.0;
        return $"{km:F2} км";
    }
}
