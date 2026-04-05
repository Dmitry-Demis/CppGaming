using cppQuest.Server.DTOs;
using cppQuest.Server.Models;
using cppQuest.Server.Repositories;
using cppQuest.Server.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Endpoints;

public static class ProgressEndpoints
{
    public static void MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        // ── Статистика чтения ────────────────────────────────────────────────
        group.MapGet("stats/avg-reading/{paragraphId}", GetAvgReadingAsync);
        group.MapPost("stats/avg-reading/bulk",         GetAvgReadingBulkAsync);

        // ── Прогресс ─────────────────────────────────────────────────────────
        group.MapGet("progress/{isuNumber}/{paragraphId}",      GetParagraphProgressAsync);
        group.MapGet("progress/{isuNumber}/unlocked-paragraphs", GetUnlockedParagraphsAsync);
        group.MapGet("progress/{isuNumber}/all",                GetAllProgressAsync);

        // ── Чтение ───────────────────────────────────────────────────────────
        group.MapPost("reading/complete", CompleteReadingAsync).RequireRateLimiting("api");
        group.MapGet("review/due",        GetReviewDueAsync);

        // ── Стрик и активность ───────────────────────────────────────────────
        group.MapGet("streak/{isuNumber}", GetStreakAsync);
        group.MapPost("scroll",            AddScrollPixelsAsync).RequireRateLimiting("api");

        // ── Тесты ────────────────────────────────────────────────────────────
        group.MapPost("test/complete", CompleteTestAsync).RequireRateLimiting("api");
    }

    // ── Handlers ────────────────────────────────────────────────────────────

    /// <summary>Возвращает среднее время чтения параграфа по всем пользователям.</summary>
    private static async Task<IResult> GetAvgReadingAsync(
        string paragraphId, IStatsRepository statsRepo) =>
        Results.Ok(new
        {
            paragraphId,
            avgSeconds = (int)await statsRepo.GetAvgReadingSecondsAsync(paragraphId)
        });

    /// <summary>То же самое, но для списка параграфов за один запрос.</summary>
    private static async Task<IResult> GetAvgReadingBulkAsync(
        List<string> paragraphIds, IStatsRepository statsRepo) =>
        Results.Ok(await statsRepo.GetAvgReadingSecondsBulkAsync(paragraphIds));

    /// <summary>Возвращает прогресс пользователя по конкретному параграфу.</summary>
    private static async Task<IResult> GetParagraphProgressAsync(
        string isuNumber, string paragraphId,
        ProfileService profileService, StatsService statsService)
    {
        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return Results.NotFound();
        return Results.Ok(await statsService.GetParagraphProgressAsync(profile.Id, paragraphId));
    }

    /// <summary>
    /// Возвращает разблокированные параграфы пользователя и флаг isAdmin.
    /// Параграф разблокирован, если минимальный лучший балл по всем его тестам ≥ 70.
    /// isAdmin берётся только из БД — клиентский заголовок игнорируется.
    /// </summary>
    private static async Task<IResult> GetUnlockedParagraphsAsync(
        string isuNumber,
        ProfileService profileService,
        IStatsRepository statsRepo,
        HttpContext ctx)
    {
        var empty = new { unlocked = (string[])[], isAdmin = false };

        if (EndpointHelpers.GetIsuNumber(ctx) is null) return Results.Ok(empty);

        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return Results.Ok(empty);

        var allTests = await statsRepo.GetAllTestStatsAsync(profile.Id);
        return Results.Ok(new { unlocked = ComputeUnlockedParagraphs(allTests), isAdmin = profile.IsAdmin });
    }

    /// <summary>Возвращает прогресс пользователя по всем параграфам.</summary>
    private static async Task<IResult> GetAllProgressAsync(
        string isuNumber, ProfileService profileService, StatsService statsService)
    {
        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return Results.NotFound();
        return Results.Ok(await statsService.GetAllProgressAsync(profile.Id));
    }

    /// <summary>Фиксирует завершение сессии чтения параграфа.</summary>
    private static async Task<IResult> CompleteReadingAsync(
        ReadingSessionRequest req,
        ProfileService profileService,
        StatsService statsService,
        IAntiforgery antiforgery,
        HttpContext ctx)
    {
        try { await antiforgery.ValidateRequestAsync(ctx); }
        catch { return Results.StatusCode(StatusCodes.Status403Forbidden); }

        var isuNumber = EndpointHelpers.GetIsuNumber(ctx);
        if (isuNumber is null) return Results.BadRequest();
        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return Results.NotFound();
        await statsService.CompleteReadingAsync(profile.Id, req);
        return Results.Ok();
    }

    /// <summary>Возвращает параграфы с вопросами, запланированными к повтору сегодня (Spaced Repetition).</summary>
    private static async Task<IResult> GetReviewDueAsync(
        ProfileService profileService, QuestionProgressService qpService, HttpContext ctx)
    {
        var isuNumber = EndpointHelpers.GetIsuNumber(ctx);
        if (isuNumber is null) return Results.Ok((object[])[]);
        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return Results.Ok((object[])[]);
        return Results.Ok(await qpService.GetDueParagraphsAsync(profile.Id));
    }

    /// <summary>Проверяет стрик при входе и начисляет ежедневную награду.</summary>
    private static async Task<IResult> GetStreakAsync(
        string isuNumber, ProfileService profileService, GamificationService gamification)
    {
        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return Results.NotFound();
        return Results.Ok(await gamification.CheckStreakOnLoginAsync(profile.Id));
    }

    /// <summary>
    /// Добавляет пиксели прокрутки к gamification-профилю пользователя.
    /// Использует bulk UPDATE без загрузки сущности в память.
    /// </summary>
    private static async Task<IResult> AddScrollPixelsAsync(
        ScrollRequest req, AppDbContext db, IAntiforgery antiforgery, HttpContext ctx)
    {
        try { await antiforgery.ValidateRequestAsync(ctx); }
        catch { return Results.StatusCode(StatusCodes.Status403Forbidden); }

        var isuNumber = EndpointHelpers.GetIsuNumber(ctx);
        if (isuNumber is null || req.Pixels <= 0) return Results.Ok();

        var user = await db.Users.FirstOrDefaultAsync(u => u.IsuNumber == isuNumber);
        if (user is null) return Results.Ok();

        await db.GamificationProfiles
            .Where(g => g.UserId == user.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(
                g => g.TotalScrollPixels,
                g => g.TotalScrollPixels + req.Pixels));

        return Results.Ok();
    }

    /// <summary>Фиксирует результат теста: обновляет SR-прогресс по вопросам и начисляет награду.</summary>
    private static async Task<IResult> CompleteTestAsync(
        TestCompleteRequest req,
        ProfileService profileService,
        StatsService statsService,
        QuestionProgressService qpService,
        IAntiforgery antiforgery,
        HttpContext ctx)
    {
        try { await antiforgery.ValidateRequestAsync(ctx); }
        catch { return Results.StatusCode(StatusCodes.Status403Forbidden); }

        var isuNumber = EndpointHelpers.GetIsuNumber(ctx);
        if (isuNumber is null) return Results.BadRequest();

        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return Results.NotFound();

        await UpdateQuestionProgressAsync(req, profile.Id, qpService);

        var reward  = await statsService.CompleteTestAsync(profile.Id, req);
        var updated = await profileService.GetProfileAsync(isuNumber);
        return Results.Ok(new { gamification = updated, reward });
    }

    // ── Вспомогательные методы ───────────────────────────────────────────────

    /// <summary>
    /// Фильтрует параграфы, у которых минимальный лучший балл среди всех тестов ≥ 70.
    /// </summary>
    private static string[] ComputeUnlockedParagraphs(IEnumerable<ParagraphTestStats> allTests) =>
        [.. allTests
            .GroupBy(t => t.ParagraphId)
            .Where(g => g.Where(t => t.AttemptsCount > 0)
                         .Select(t => (int?)t.BestScore)
                         .DefaultIfEmpty(null)
                         .Min() >= 70)
            .Select(g => g.Key)];

    /// <summary>
    /// Собирает словарь <c>questionId → wasCorrect</c> из двух списков запроса
    /// и передаёт его в сервис Spaced Repetition.
    /// </summary>
    private static async Task UpdateQuestionProgressAsync(
        TestCompleteRequest req, int userId, QuestionProgressService qpService)
    {
        Dictionary<int, bool> answers = [];
        foreach (var id in req.CorrectQuestionIds ?? []) answers[id] = true;
        foreach (var id in req.WrongQuestionIds   ?? []) answers[id] = false;

        if (answers.Count > 0)
            await qpService.UpdateProgressAsync(userId, req.TestId, req.ParagraphId, answers);
    }
}
