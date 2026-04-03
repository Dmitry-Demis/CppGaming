using System.Text.Json;
using cppQuest.Server.DTOs;

namespace cppQuest.Server.Services;

public class QuizService : IQuizService
{
    private readonly string _testsBasePath;

    public QuizService(IWebHostEnvironment env)
    {
        _testsBasePath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "tests"));
    }

    public async Task<QuizResponse?> GetQuizAsync(string quizId)
    {
        var file = FindTestFile(quizId);
        if (file is null) return null;

        var json = await File.ReadAllTextAsync(file);
        if (string.IsNullOrWhiteSpace(json)) return null;

        JsonDocument doc;
        try { doc = JsonDocument.Parse(json); }
        catch { return null; }

        var root = doc.RootElement;

        var questions = root.GetProperty("questions").EnumerateArray().Select(q =>
        {
            var type = q.GetProperty("type").GetString()!;
            var code = q.TryGetProperty("code", out var c) ? c.GetString() : null;
            var answers = q.TryGetProperty("answers", out var ans)
                ? ans.EnumerateArray().Select(a => a.GetString()!).ToList()
                : null;
            var explanation = q.TryGetProperty("explanation", out var ex) ? ex.GetString() : null;
            var std = q.TryGetProperty("std", out var stdProp) ? stdProp.GetString() : null;

            List<QuizPairDto>? pairs = null;
            if (q.TryGetProperty("pairs", out var pairsEl) && pairsEl.ValueKind == JsonValueKind.Array)
            {
                pairs = pairsEl.EnumerateArray().Select(p => new QuizPairDto(
                    p.GetProperty("left").GetString()!,
                    p.GetProperty("right").GetString()!
                )).ToList();
            }

            // items и categories для типа classify
            List<string>? items = null;
            if (q.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
                items = itemsEl.EnumerateArray().Select(x => x.GetString()!).ToList();

            List<string>? categories = null;
            if (q.TryGetProperty("categories", out var catsEl) && catsEl.ValueKind == JsonValueKind.Array)
                categories = catsEl.EnumerateArray().Select(x => x.GetString()!).ToList();

            object? correct = null;
            if (q.TryGetProperty("correct", out var corr))
            {
                correct = ParseCorrect(corr);
            }

            return new QuizQuestionDto(
                q.GetProperty("id").GetInt32(),
                type,
                q.GetProperty("question").GetString()!,
                code,
                answers,
                correct,
                explanation,
                std,
                pairs,
                items,
                categories
            );
        }).ToList();

        return new QuizResponse(
            root.TryGetProperty("quizId", out var qid) ? qid.GetString()! :
                root.TryGetProperty("id", out JsonElement id) ? id.GetString()! : quizId,
            root.GetProperty("title").GetString()!,
            root.TryGetProperty("type", out var tp) ? tp.GetString()! : "mini",
            root.TryGetProperty("passingScore", out var ps) ? ps.GetInt32() : 70,
            root.TryGetProperty("pick", out var pk) ? pk.GetInt32() : questions.Count,
            questions
        );
    }

    public async Task<QuizSubmitResponse?> SubmitQuizAsync(string quizId, QuizSubmitRequest request)
    {
        var file = FindTestFile(quizId);
        if (file is null) return null;

        var json = await File.ReadAllTextAsync(file);
        if (string.IsNullOrWhiteSpace(json)) return null;

        JsonDocument doc;
        try { doc = JsonDocument.Parse(json); }
        catch { return null; }

        var root = doc.RootElement;
        var passingScore = root.TryGetProperty("passingScore", out var ps) ? ps.GetInt32() : 70;

        var results = new List<QuizResultDto>();
        int earned = 0;
        int total = 0;

        foreach (var q in root.GetProperty("questions").EnumerateArray())
        {
            total++;
            var id = q.GetProperty("id").GetInt32();
            var type = q.GetProperty("type").GetString()!;
            var explanation = q.TryGetProperty("explanation", out var ex) ? ex.GetString()! : "";

            request.Answers.TryGetValue(id.ToString(), out var submitted);

            bool isRight = false;
            int pts = 0;

            if (type == "single" || type == "code")
            {
                var correct = q.GetProperty("correct").GetInt32();
                isRight = submitted is JsonElement el
                    && el.ValueKind == JsonValueKind.Number
                    && el.GetInt32() == correct;
                pts = isRight ? 10 : 0;
            }
            else if (type == "multiple")
            {
                var correct = q.GetProperty("correct").EnumerateArray()
                    .Select(x => x.GetInt32()).OrderBy(x => x).ToList();
                List<int> userList = [];
                if (submitted is JsonElement arr && arr.ValueKind == JsonValueKind.Array)
                    userList = arr.EnumerateArray().Select(x => x.GetInt32()).OrderBy(x => x).ToList();
                isRight = correct.SequenceEqual(userList);
                var partial = correct.Any(c => userList.Contains(c));
                pts = isRight ? 10 : partial ? 5 : 0;
            }
            else if (type == "fill")
            {
                var accepted = q.GetProperty("correct").EnumerateArray()
                    .Select(x => x.GetString()?.ToLower()).ToList();
                var userVal = submitted is JsonElement sv ? sv.GetString()?.Trim().ToLower() : null;
                isRight = userVal != null && accepted.Contains(userVal);
                pts = isRight ? 10 : 0;
            }

            earned += pts;
            results.Add(new QuizResultDto(id, isRight, pts, explanation, q.GetProperty("correct")));
        }

        int maxScore = total * 10;
        int pct = maxScore > 0 ? (int)Math.Round((double)earned / maxScore * 100) : 0;
        bool passed = pct >= passingScore;

        return new QuizSubmitResponse(pct, passed, earned, maxScore, results);
    }

    // Рекурсивно парсит correct: число, строка, массив строк/чисел, массив массивов, null
    private static object? ParseCorrect(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.Null      => null,
            JsonValueKind.Number    => el.GetInt32(),
            JsonValueKind.String    => el.GetString(),
            JsonValueKind.Array     => el.EnumerateArray()
                                        .Select(ParseCorrect)
                                        .ToList(),
            _                       => null
        };
    }

    private string? FindTestFile(string quizId)
    {
        if (!Directory.Exists(_testsBasePath)) return null;

        // First try exact filename match (fast path)
        var byName = Directory.EnumerateFiles(_testsBasePath, $"{quizId}.json", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (byName != null) return byName;

        // Fallback: search by quizId field inside JSON
        return Directory.EnumerateFiles(_testsBasePath, "*.json", SearchOption.AllDirectories)
            .FirstOrDefault(file =>
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(file));
                    return doc.RootElement.TryGetProperty("quizId", out var qid) && qid.GetString() == quizId;
                }
                catch { return false; }
            });
    }
}
