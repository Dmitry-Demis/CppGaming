using Microsoft.EntityFrameworkCore;
using cppQuest.Server.Models;
using cppQuest.Server.Services;
using Microsoft.AspNetCore.Antiforgery;

namespace cppQuest.Server.Endpoints;

public static class AchievementEndpoints
{
    public static void MapAchievementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/achievements");

        // GET /api/achievements/{isuNumber}
        // Возвращает список разблокированных достижений с датами
        group.MapGet("{isuNumber}", async (string isuNumber, ProfileService profileService, AppDbContext db) =>
        {
            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.NotFound();

            var achievements = await db.UserAchievements
                .Where(a => a.UserId == profile.Id)
                .Select(a => new { a.AchievementId, a.UnlockedAt })
                .ToListAsync();

            return Results.Ok(achievements);
        });

        // POST /api/achievements/{isuNumber}/unlock
        // Разблокирует достижение (идемпотентно)
        group.MapPost("{isuNumber}/unlock", async (
            string isuNumber,
            UnlockAchievementRequest req,
            ProfileService profileService,
            IAntiforgery antiforgery,
            AppDbContext db,
            HttpContext ctx) =>
        {
            try { await antiforgery.ValidateRequestAsync(ctx); }
            catch { return Results.StatusCode(StatusCodes.Status403Forbidden); }

            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.NotFound();

            var exists = await db.UserAchievements
                .AnyAsync(a => a.UserId == profile.Id && a.AchievementId == req.AchievementId);

            if (!exists)
            {
                db.UserAchievements.Add(new UserAchievement
                {
                    UserId = profile.Id,
                    AchievementId = req.AchievementId,
                    UnlockedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { unlocked = true, achievementId = req.AchievementId });
        });

        // POST /api/achievements/{isuNumber}/unlock-batch
        // Разблокирует несколько достижений за раз (для миграции из localStorage)
        group.MapPost("{isuNumber}/unlock-batch", async (
            string isuNumber,
            UnlockBatchRequest req,
            ProfileService profileService,
            IAntiforgery antiforgery,
            AppDbContext db,
            HttpContext ctx) =>
        {
            try { await antiforgery.ValidateRequestAsync(ctx); }
            catch { return Results.StatusCode(StatusCodes.Status403Forbidden); }

            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.NotFound();

            var existing = await db.UserAchievements
                .Where(a => a.UserId == profile.Id)
                .Select(a => a.AchievementId)
                .ToHashSetAsync();

            var toAdd = req.Achievements
                .Where(a => !existing.Contains(a.AchievementId))
                .Select(a => new UserAchievement
                {
                    UserId = profile.Id,
                    AchievementId = a.AchievementId,
                    UnlockedAt = a.UnlockedAt ?? DateTime.UtcNow
                })
                .ToList();

            if (toAdd.Count > 0)
            {
                db.UserAchievements.AddRange(toAdd);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { migrated = toAdd.Count });
        });
    }
}

public record UnlockAchievementRequest(string AchievementId);
public record AchievementEntry(string AchievementId, DateTime? UnlockedAt);
public record UnlockBatchRequest(List<AchievementEntry> Achievements);
