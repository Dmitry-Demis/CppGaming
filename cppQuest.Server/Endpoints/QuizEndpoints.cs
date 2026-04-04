using cppQuest.Server.DTOs;
using cppQuest.Server.Models;
using cppQuest.Server.Repositories;
using cppQuest.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Endpoints;

public static class QuizEndpoints
{
    public static void MapQuizEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quiz");

        // GET /api/quiz/list — список всех тестов (id + title + type)
        group.MapGet("list", (IWebHostEnvironment env) =>
        {
            var testsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "tests"));
            if (!Directory.Exists(testsRoot)) return Results.Ok(Array.Empty<object>());

            var list = Directory.EnumerateFiles(testsRoot, "*.json", SearchOption.AllDirectories)
                .Select(file =>
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
                        var root = doc.RootElement;
                        var id = root.TryGetProperty("quizId", out var qid) ? qid.GetString()
                               : root.TryGetProperty("id", out var rid) ? rid.GetString()
                               : Path.GetFileNameWithoutExtension(file);
                        var title = root.TryGetProperty("title", out var t) ? t.GetString() : id;
                        var type  = root.TryGetProperty("type",  out var tp) ? tp.GetString() : "mini";
                        var pick  = root.TryGetProperty("pick",  out var pk) ? pk.GetInt32() : 0;
                        var total = root.TryGetProperty("questions", out var qs) && qs.ValueKind == System.Text.Json.JsonValueKind.Array
                            ? qs.GetArrayLength() : 0;
                        return new { id, title, type, pick, total };
                    }
                    catch { return null; }
                })
                .Where(x => x != null)
                .OrderBy(x => x!.id)
                .ToList();

            return Results.Ok(list);
        });

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
                Questions = [.. quiz.Questions.Where(q => q.Std is null || unlockedStds.Contains(q.Std))]
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

        // GET /api/quiz/{quizId}/question-stats/{questionId} — % правильных ответов на вопрос
        group.MapGet("{quizId}/question-stats/{questionId:int}", async (
            string quizId,
            int questionId,
            AppDbContext db) =>
        {
            var attempts = await db.TestAttempts
                .Where(a => a.TestId == quizId)
                .Select(a => new { a.CorrectQuestionIds, a.WrongQuestionIds })
                .ToListAsync();

            int total = 0, correct = 0;
            foreach (var a in attempts)
            {
                bool wasCorrect = false, wasSeen = false;
                if (!string.IsNullOrEmpty(a.CorrectQuestionIds))
                {
                    try
                    {
                        var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(a.CorrectQuestionIds);
                        if (ids?.Contains(questionId) == true) { wasCorrect = true; wasSeen = true; }
                    }
                    catch { }
                }
                if (!string.IsNullOrEmpty(a.WrongQuestionIds))
                {
                    try
                    {
                        var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(a.WrongQuestionIds);
                        if (ids?.Contains(questionId) == true) wasSeen = true;
                    }
                    catch { }
                }
                if (wasSeen) { total++; if (wasCorrect) correct++; }
            }

            if (total == 0) return Results.Ok(new { totalAttempts = 0, correctPct = 0 });
            return Results.Ok(new { totalAttempts = total, correctPct = (int)Math.Round(correct * 100.0 / total) });
        });

        // GET /api/quiz/{quizId}/score-distribution — распределение результатов
        group.MapGet("{quizId}/score-distribution", async (
            string quizId,
            ProfileService profileService,
            AppDbContext db,
            HttpContext ctx) =>
        {
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();

            // Если передан query ?score=N — используем его (для экрана результатов сразу после теста)
            int? overrideScore = null;
            if (ctx.Request.Query.TryGetValue("score", out var sv) && int.TryParse(sv, out var parsedScore))
                overrideScore = parsedScore;

            int? myLast = overrideScore;
            if (myLast == null && !string.IsNullOrEmpty(isuNumber))
            {
                var profile = await profileService.GetProfileAsync(isuNumber);
                if (profile != null)
                {
                    var myStats = await db.ParagraphTestStats
                        .Where(s => s.UserId == profile.Id && s.TestId == quizId)
                        .FirstOrDefaultAsync();
                    myLast = myStats?.LastScore;
                }
            }

            // Берём последний результат каждого пользователя
            var lastScores = await db.ParagraphTestStats
                .Where(s => s.TestId == quizId && s.AttemptsCount > 0)
                .Select(s => s.LastScore)
                .ToListAsync();

            if (lastScores.Count == 0)
                return Results.Ok(new { totalAttempts = 0, pctAbove = 0, pctSame = 0, pctBelow = 0 });

            int score = myLast ?? 0;
            int above = lastScores.Count(s => s > score);
            int same  = lastScores.Count(s => s == score);
            int below = lastScores.Count(s => s < score);
            int total = lastScores.Count;

            return Results.Ok(new
            {
                totalAttempts = total,
                pctAbove = (int)Math.Round(above * 100.0 / total),
                pctSame  = (int)Math.Round(same  * 100.0 / total),
                pctBelow = (int)Math.Round(below * 100.0 / total),
                myScore  = score,
            });
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
