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

        // GET /api/quiz/list — список всех тестов (id, title, type, pick, total)
        group.MapGet("list", ListQuizzesAsync);

        // GET /api/quiz/{quizId} — тест с вопросами, отфильтрованными по купленным стандартам
        group.MapGet("{quizId}", GetQuizAsync);

        // POST /api/quiz/{quizId}/submit — проверка ответов
        group.MapPost("{quizId}/submit", SubmitQuizAsync);

        // GET /api/quiz/{quizId}/wrong-ids — ID вопросов, на которые пользователь ответил неверно в последний раз
        group.MapGet("{quizId}/wrong-ids", GetWrongIdsAsync);

        // GET /api/quiz/{quizId}/question-stats/{questionId} — % правильных ответов на конкретный вопрос
        group.MapGet("{quizId}/question-stats/{questionId:int}", GetQuestionStatsAsync);

        // GET /api/quiz/{quizId}/score-distribution — распределение результатов всех пользователей
        group.MapGet("{quizId}/score-distribution", GetScoreDistributionAsync);

        // GET /api/quiz/{quizId}/pick-questions — SR-выборка вопросов с учётом купленных стандартов
        group.MapGet("{quizId}/pick-questions", PickQuestionsAsync);
    }

    // ── Handlers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Сканирует директорию <c>tests/</c> и возвращает метаданные всех найденных тестов.
    /// Файлы, которые не удаётся распарсить, молча пропускаются.
    /// </summary>
    private static IResult ListQuizzesAsync(IWebHostEnvironment env)
    {
        var testsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "tests"));
        if (!Directory.Exists(testsRoot)) return Results.Ok((object[])[]);

        var list = Directory.EnumerateFiles(testsRoot, "*.json", SearchOption.AllDirectories)
            .Select(file =>
            {
                try
                {
                    using var doc  = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
                    var root  = doc.RootElement;
                    // quizId: явное поле → fallback на "id" → fallback на имя файла
                    var id    = root.TryGetProperty("quizId", out var qid) ? qid.GetString()
                              : root.TryGetProperty("id",     out var rid) ? rid.GetString()
                              : Path.GetFileNameWithoutExtension(file);
                    var title = root.TryGetProperty("title", out var t)  ? t.GetString()  : id;
                    var type  = root.TryGetProperty("type",  out var tp) ? tp.GetString() : "mini";
                    var pick  = root.TryGetProperty("pick",  out var pk) ? pk.GetInt32()  : 0;
                    var total = root.TryGetProperty("questions", out var qs) &&
                                qs.ValueKind == System.Text.Json.JsonValueKind.Array
                                ? qs.GetArrayLength() : 0;
                    return new { id, title, type, pick, total };
                }
                catch { return null; }
            })
            .Where(x => x != null)
            .OrderBy(x => x!.id)
            .ToList();

        return Results.Ok(list);
    }

    /// <summary>
    /// Возвращает тест, фильтруя вопросы по купленным стандартам пользователя.
    /// Вопросы без стандарта (<c>std == null</c>) видны всем.
    /// </summary>
    private static async Task<IResult> GetQuizAsync(
        string quizId,
        IQuizService quizService,
        ProfileService profileService,
        IShopRepository shopRepo,
        HttpContext ctx)
    {
        var quiz = await quizService.GetQuizAsync(quizId);
        if (quiz is null) return Results.NotFound(new { message = $"Quiz '{quizId}' not found" });

        var unlockedStds = await EndpointHelpers.GetUnlockedStdsAsync(profileService, shopRepo, ctx);

        var filtered = quiz with
        {
            Questions = [.. quiz.Questions.Where(q => q.Std is null || unlockedStds.Contains(q.Std))]
        };

        return Results.Ok(filtered);
    }

    private static async Task<IResult> SubmitQuizAsync(
        string quizId,
        QuizSubmitRequest req,
        IQuizService quizService,
        HttpContext ctx)
    {
        var result = await quizService.SubmitQuizAsync(quizId, req);
        return result is not null
            ? Results.Ok(result)
            : Results.NotFound(new { message = $"Quiz '{quizId}' not found" });
    }

    private static async Task<IResult> GetWrongIdsAsync(
        string quizId,
        ProfileService profileService,
        IStatsRepository statsRepo,
        HttpContext ctx)
    {
        var isuNumber = EndpointHelpers.GetIsuNumber(ctx);
        if (isuNumber is null) return Results.Ok((int[])[]);

        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return Results.Ok((int[])[]);

        return Results.Ok(await statsRepo.GetLastWrongQuestionIdsAsync(profile.Id, quizId));
    }

    /// <summary>
    /// Считает процент правильных ответов на конкретный вопрос по всем попыткам в системе.
    /// Вопрос считается "виденным" в попытке, если он есть в correctIds или wrongIds.
    /// </summary>
    /// <param name="questionId">ID вопроса внутри теста.</param>
    private static async Task<IResult> GetQuestionStatsAsync(
        string quizId,
        int questionId,
        AppDbContext db)
    {
        var attempts = await db.TestAttempts
            .Where(a => a.TestId == quizId)
            .Select(a => new AttemptIds(a.CorrectQuestionIds, a.WrongQuestionIds))
            .ToListAsync();

        var (total, correct) = CountQuestionHits(attempts, questionId);

        if (total == 0) return Results.Ok(new { totalAttempts = 0, correctPct = 0 });
        return Results.Ok(new
        {
            totalAttempts = total,
            correctPct    = (int)Math.Round(correct * 100.0 / total)
        });
    }

    /// <summary>
    /// Возвращает распределение последних результатов всех пользователей по тесту.
    /// Если передан query-параметр <c>?score=N</c>, он используется как результат текущего пользователя
    /// (удобно сразу после прохождения теста, до сохранения в БД).
    /// </summary>
    private static async Task<IResult> GetScoreDistributionAsync(
        string quizId,
        ProfileService profileService,
        AppDbContext db,
        HttpContext ctx)
    {
        var myScore = await ResolveMyScoreAsync(quizId, profileService, db, ctx);

        var lastScores = await db.ParagraphTestStats
            .Where(s => s.TestId == quizId && s.AttemptsCount > 0)
            .Select(s => s.LastScore)
            .ToListAsync();

        if (lastScores.Count == 0)
            return Results.Ok(new { totalAttempts = 0, pctAbove = 0, pctSame = 0, pctBelow = 0 });

        int score = myScore ?? 0;
        int total = lastScores.Count;
        return Results.Ok(new
        {
            totalAttempts = total,
            pctAbove = (int)Math.Round(lastScores.Count(s => s > score)  * 100.0 / total),
            pctSame  = (int)Math.Round(lastScores.Count(s => s == score) * 100.0 / total),
            pctBelow = (int)Math.Round(lastScores.Count(s => s < score)  * 100.0 / total),
            myScore  = score,
        });
    }

    /// <summary>
    /// Выбирает вопросы для прохождения теста с учётом Spaced Repetition.
    /// Для анонимных пользователей — случайная выборка.
    /// Фильтрует вопросы по купленным стандартам.
    /// </summary>
    /// <param name="quizId">Идентификатор теста.</param>
    private static async Task<IResult> PickQuestionsAsync(
        string quizId,
        IQuizService quizService,
        ProfileService profileService,
        QuestionProgressService qpService,
        IShopRepository shopRepo,
        HttpContext ctx)
    {
        var quiz = await quizService.GetQuizAsync(quizId);
        if (quiz is null) return Results.NotFound();

        var unlockedStds = await EndpointHelpers.GetUnlockedStdsAsync(profileService, shopRepo, ctx);

        var availableIds = quiz.Questions
            .Where(q => q.Std is null || unlockedStds.Contains(q.Std))
            .Select(q => q.Id)
            .ToList();

        // pick=0 означает "все вопросы"
        int pick = quiz.Pick > 0 ? Math.Min(quiz.Pick, availableIds.Count) : availableIds.Count;

        var isuNumber = EndpointHelpers.GetIsuNumber(ctx);
        if (isuNumber is null)
            return Results.Ok(RandomPick(availableIds, pick));

        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null)
            return Results.Ok(RandomPick(availableIds, pick));

        return Results.Ok(await qpService.PickQuestionIdsAsync(profile.Id, quizId, availableIds, pick));
    }

    // ── Вспомогательные методы ───────────────────────────────────────────────

    /// <summary>
    /// Случайная выборка <paramref name="count"/> элементов из списка.
    /// </summary>
    private static List<int> RandomPick(List<int> ids, int count) =>
        ids.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();

    private record AttemptIds(string? CorrectQuestionIds, string? WrongQuestionIds);

    /// <summary>
    /// Десериализует JSON-строку с массивом int.
    /// Возвращает <c>false</c> при пустой строке или ошибке парсинга.
    /// </summary>
    private static bool TryDeserializeIds(string? json, out List<int>? ids)
    {
        ids = null;
        if (string.IsNullOrEmpty(json)) return false;
        try
        {
            ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(json);
            return ids is not null;
        }
        catch { return false; }
    }

    /// <summary>
    /// Определяет счёт текущего пользователя для score-distribution.
    /// Приоритет: query-параметр <c>?score=N</c> → последний результат из БД.
    /// </summary>
    private static async Task<int?> ResolveMyScoreAsync(
        string quizId, ProfileService profileService, AppDbContext db, HttpContext ctx)
    {
        // Явный score из query — используем сразу (экран результатов сразу после теста)
        if (ctx.Request.Query.TryGetValue("score", out var sv) && int.TryParse(sv, out var parsed))
            return parsed;

        var isuNumber = EndpointHelpers.GetIsuNumber(ctx);
        if (isuNumber is null) return null;

        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return null;

        var myStats = await db.ParagraphTestStats
            .Where(s => s.UserId == profile.Id && s.TestId == quizId)
            .FirstOrDefaultAsync();
        return myStats?.LastScore;
    }

    /// <summary>
    /// Подсчитывает, сколько раз вопрос был виден и сколько раз отвечен правильно.
    /// </summary>
    /// <param name="attempts">Список попыток с сериализованными списками ID.</param>
    /// <param name="questionId">ID вопроса для поиска.</param>
    /// <returns>Кортеж (total виденных, correct правильных).</returns>
    private static (int total, int correct) CountQuestionHits(
        IEnumerable<AttemptIds> attempts, int questionId)
    {
        int total = 0, correct = 0;
        foreach (var a in attempts)
        {
            bool wasCorrect = TryDeserializeIds(a.CorrectQuestionIds, out List<int>? cIds) && cIds!.Contains(questionId);
            bool wasSeen    = wasCorrect
                           || (TryDeserializeIds(a.WrongQuestionIds, out List<int>? wIds) && wIds!.Contains(questionId));

            if (!wasSeen) continue;
            total++;
            if (wasCorrect) correct++;
        }
        return (total, correct);
    }
}
