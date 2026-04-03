using cppQuest.Server.Models;
using cppQuest.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Endpoints;

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/feedback");

        // POST /api/feedback — сохранить отзыв
        group.MapPost("", async (FeedbackRequest req, AppDbContext db,
                                 ProfileService profileService, HttpContext ctx) =>
        {
            if (req.Rating < 1 || req.Rating > 10)
                return Results.BadRequest("Rating must be 1–10.");
            if (req.Comment?.Length > 1024)
                return Results.BadRequest("Comment too long.");

            int? userId = null;
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            if (!string.IsNullOrEmpty(isuNumber))
            {
                var profile = await profileService.GetProfileAsync(isuNumber);
                userId = profile?.Id;
            }

            db.PageFeedbacks.Add(new PageFeedback
            {
                UserId  = userId,
                PageId  = req.PageId.Trim(),
                Rating  = req.Rating,
                Comment = string.IsNullOrWhiteSpace(req.Comment) ? null : req.Comment.Trim(),
            });

            await db.SaveChangesAsync();
            return Results.Ok();
        });

        // GET /api/feedback/{pageId} — общая статистика + оценка текущего пользователя
        group.MapGet("{pageId}", async (string pageId, AppDbContext db,
                                        ProfileService profileService, HttpContext ctx) =>
        {
            var ratings = await db.PageFeedbacks
                .Where(f => f.PageId == pageId)
                .Select(f => new { f.UserId, f.Rating })
                .ToListAsync();

            double? average = ratings.Count > 0
                ? Math.Round(ratings.Average(r => r.Rating), 1)
                : null;

            int? myRating = null;
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            if (!string.IsNullOrEmpty(isuNumber))
            {
                var profile = await profileService.GetProfileAsync(isuNumber);
                if (profile is not null)
                    myRating = ratings
                        .Where(r => r.UserId == profile.Id)
                        .Select(r => (int?)r.Rating)
                        .FirstOrDefault();
            }

            return Results.Ok(new
            {
                count   = ratings.Count,
                average,
                myRating,
            });
        });
    }

    private record FeedbackRequest(string PageId, int Rating, string? Comment);
}
