using cppQuest.Server.DTOs;
using cppQuest.Server.Repositories;
using cppQuest.Server.Services;

namespace cppQuest.Server.Endpoints;

public static class QuizEndpoints
{
    public static void MapQuizEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quiz");

        // GET /api/quiz/{quizId} — возвращает тест, фильтруя вопросы по купленным стандартам
        group.MapGet("{quizId}", async (
            string quizId,
            IQuizService quizService,
            ProfileService profileService,
            IShopRepository shopRepo,
            HttpContext ctx) =>
        {
            var quiz = await quizService.GetQuizAsync(quizId);
            if (quiz is null) return Results.NotFound(new { message = $"Quiz '{quizId}' not found" });

            // Определяем купленные стандарты пользователя
            var unlockedStds = await GetUnlockedStds(profileService, shopRepo, ctx);

            // Фильтруем вопросы: показываем только те, у которых std == null или std куплен
            var filtered = quiz with
            {
                Questions = quiz.Questions
                    .Where(q => q.Std is null || unlockedStds.Contains(q.Std))
                    .ToList()
            };

            return Results.Ok(filtered);
        });

        group.MapPost("{quizId}/submit", async (
            string quizId,
            QuizSubmitRequest req,
            IQuizService quizService,
            ProfileService profileService,
            IShopRepository shopRepo,
            HttpContext ctx) =>
        {
            var result = await quizService.SubmitQuizAsync(quizId, req);
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(new { message = $"Quiz '{quizId}' not found" });
        });

        // GET /api/quiz/{quizId}/wrong-ids
        group.MapGet("{quizId}/wrong-ids", async (
            string quizId,
            ProfileService profileService,
            IStatsRepository statsRepo,
            HttpContext ctx) =>
        {
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            if (string.IsNullOrEmpty(isuNumber)) return Results.Ok(Array.Empty<int>());
            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.Ok(Array.Empty<int>());
            var wrongIds = await statsRepo.GetLastWrongQuestionIdsAsync(profile.Id, quizId);
            return Results.Ok(wrongIds);
        });

        // GET /api/quiz/{quizId}/pick-questions — SR-выборка с учётом купленных стандартов
        group.MapGet("{quizId}/pick-questions", async (
            string quizId,
            IQuizService quizService,
            ProfileService profileService,
            QuestionProgressService qpService,
            IShopRepository shopRepo,
            HttpContext ctx) =>
        {
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();

            var quiz = await quizService.GetQuizAsync(quizId);
            if (quiz is null) return Results.NotFound();

            // Фильтруем по купленным стандартам
            var unlockedStds = await GetUnlockedStds(profileService, shopRepo, ctx);
            var availableIds = quiz.Questions
                .Where(q => q.Std is null || unlockedStds.Contains(q.Std))
                .Select(q => q.Id)
                .ToList();

            int pick = quiz.Pick > 0 ? Math.Min(quiz.Pick, availableIds.Count) : availableIds.Count;

            if (string.IsNullOrEmpty(isuNumber))
            {
                var rng = new Random();
                return Results.Ok(availableIds.OrderBy(_ => rng.Next()).Take(pick).ToList());
            }

            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null)
            {
                var rng = new Random();
                return Results.Ok(availableIds.OrderBy(_ => rng.Next()).Take(pick).ToList());
            }

            var picked = await qpService.PickQuestionIdsAsync(profile.Id, quizId, availableIds, pick);
            return Results.Ok(picked);
        });
    }

    // Возвращает Set купленных стандартов: {"11", "14", "17", ...}
    // C++98 бесплатен всегда
    private static async Task<HashSet<string>> GetUnlockedStds(
        ProfileService profileService,
        IShopRepository shopRepo,
        HttpContext ctx)
    {
        var result = new HashSet<string> { "98" };
        var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
        if (string.IsNullOrEmpty(isuNumber)) return result;

        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return result;

        var purchases = await shopRepo.GetUserPurchasesAsync(profile.Id);
        foreach (var p in purchases.Where(p => p.ItemId.StartsWith("std_cpp")))
            result.Add(p.ItemId.Replace("std_cpp", ""));

        return result;
    }
}
