using cppQuest.Server.Models;
using cppQuest.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Endpoints;

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/feedback");

        // POST /api/feedback — сохранить отзыв на страницу
        group.MapPost("", SaveFeedbackAsync);

        // GET /api/feedback/{pageId} — статистика страницы + оценка текущего пользователя
        group.MapGet("{pageId}", GetFeedbackAsync);
    }

    /// <summary>
    /// Сохраняет отзыв пользователя на страницу курса.
    /// Пользователь определяется по заголовку <c>X-Isu-Number</c>; анонимные отзывы тоже принимаются.
    /// </summary>
    /// <param name="req">Тело запроса: pageId, rating (1–10), опциональный комментарий.</param>
    private static async Task<IResult> SaveFeedbackAsync(
        FeedbackRequest req,
        AppDbContext db,
        ProfileService profileService,
        HttpContext ctx)
    {
        // ── Валидация ────────────────────────────────────────────────────────
        if (ValidateFeedback(req) is { } validationError) return validationError;

        // ── Бизнес-логика ────────────────────────────────────────────────────
        // Пытаемся привязать отзыв к пользователю, но не требуем авторизации
        int? userId = null;
        var isuNumber = EndpointHelpers.GetIsuNumber(ctx);
        if (isuNumber is not null)
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
    }

    /// <summary>
    /// Возвращает агрегированную статистику отзывов страницы и оценку текущего пользователя.
    /// </summary>
    /// <param name="pageId">Идентификатор страницы курса.</param>
    private static async Task<IResult> GetFeedbackAsync(
        string pageId,
        AppDbContext db,
        ProfileService profileService,
        HttpContext ctx)
    {
        var ratings = await db.PageFeedbacks
            .Where(f => f.PageId == pageId)
            .Select(f => new { f.UserId, f.Rating })
            .ToListAsync();

        double? average = ratings.Count > 0
            ? Math.Round(ratings.Average(r => r.Rating), 1)
            : null;

        int? myRating = await ResolveMyRatingAsync(ratings, profileService, ctx);

        return Results.Ok(new { count = ratings.Count, average, myRating });
    }

    // ── Валидация ────────────────────────────────────────────────────────────

    /// <summary>
    /// Проверяет корректность полей отзыва.
    /// Возвращает <c>IResult</c> с ошибкой или <c>null</c>, если всё в порядке.
    /// </summary>
    private static IResult? ValidateFeedback(FeedbackRequest req)
    {
        if (req.Rating < 1 || req.Rating > 10)
            return Results.BadRequest("Rating must be 1–10.");
        if (req.Comment?.Length > 1024)
            return Results.BadRequest("Comment too long.");
        return null;
    }

    // ── Вспомогательные методы ───────────────────────────────────────────────

    /// <summary>
    /// Находит оценку текущего пользователя среди уже загруженных рейтингов.
    /// Возвращает <c>null</c>, если пользователь анонимный или ещё не оставлял отзыв.
    /// </summary>
    private static async Task<int?> ResolveMyRatingAsync(
        IEnumerable<dynamic> ratings,
        ProfileService profileService,
        HttpContext ctx)
    {
        var isuNumber = EndpointHelpers.GetIsuNumber(ctx);
        if (isuNumber is null) return null;

        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return null;

        return ratings
            .Where(r => r.UserId == profile.Id)
            .Select(r => (int?)r.Rating)
            .FirstOrDefault();
    }

    private record FeedbackRequest(string PageId, int Rating, string? Comment);
}
