using System.Text.Json;
using cppQuest.Server.DTOs;
using cppQuest.Server.Models;
using cppQuest.Server.Repositories;

namespace cppQuest.Server.Services;

public class StatsService(
    IStatsRepository statsRepo,
    GamificationService gamification,
    IQuizService quizService,
    IQuestionProgressRepository questionProgressRepo)
{
    // XP/монеты за чтение
    private const int ReadXp = 15;
    private const int ReadCoins = 5;

    // Определяет статус по проценту и типу теста
    private static int CalcStatus(int pct, int totalQuestions) =>
        totalQuestions switch
        {
            5  => pct switch { 100 => 4, >= 80 => 2, >= 60 => 1, _ => 0 },  // мини: нет серебра
            10 => pct switch { 100 => 4, >= 90 => 3, >= 80 => 2, >= 70 => 1, _ => 0 },
            20 => pct switch { 100 => 4, >= 90 => 3, >= 80 => 2, >= 70 => 1, _ => 0 },
            _  => pct switch { 100 => 4, >= 90 => 3, >= 80 => 2, >= 70 => 1, _ => 0 }  // 40 и другие
        };

    private static string StatusName(int status) => status switch
    {
        4 => "gold", 3 => "silver", 2 => "bronze", 1 => "passed", _ => "failed"
    };

    // XP за достижение нового статуса (накопительно от предыдущего)
    private static int CalcXpForNewStatus(int prevStatus, int newStatus, int totalQuestions)
    {
        if (newStatus <= prevStatus) return 0;

        // XP таблицы по типу теста
        int[] xpTable = totalQuestions switch
        {
            5  => [0, 10, 13, 13, 25],   // [none, passed, bronze, silver(нет), gold]
            10 => [0, 15, 22, 32, 45],
            20 => [0, 25, 40, 60, 83],
            _  => [0, 35, 55, 80, 110]   // 40 вопросов (chapter)
        };

        // Разница между новым и предыдущим накопленным XP
        return xpTable[newStatus] - xpTable[prevStatus];
    }

    // Монеты за вопрос по типу теста
    private static int CoinsPerQuestion(int totalQuestions) => totalQuestions switch
    {
        5  => 10,
        10 => 15,
        20 => 20,
        _  => 25  // 40 вопросов (chapter)
    };

    public async Task<TestRewardResponse> CompleteTestAsync(int userId, TestCompleteRequest dto)
    {
        // 1. Обновляем агрегированную статистику теста
        var testStats = await statsRepo.GetTestStatsAsync(userId, dto.TestId)
            ?? new ParagraphTestStats
            {
                UserId = userId,
                TestId = dto.TestId,
                ParagraphId = dto.ParagraphId
            };

        bool isFirstAttempt = testStats.AttemptsCount == 0;

        testStats.AttemptsCount++;
        testStats.TotalScoreSum += dto.Score;
        testStats.LastScore = dto.Score;
        if (dto.Score > testStats.BestScore)
            testStats.BestScore = dto.Score;
        testStats.LastAttemptAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dto.TestTitle) && string.IsNullOrEmpty(testStats.TestTitle))
            testStats.TestTitle = dto.TestTitle;

        // 2. Считаем монеты — только за новые правильные вопросы
        var correctIds = dto.CorrectQuestionIds ?? [];
        var everCorrect = await statsRepo.GetEverCorrectQuestionIdsAsync(userId, dto.TestId);
        var newlyCorrect = correctIds.Where(id => !everCorrect.Contains(id)).ToList();
        int coinsPerQ = CoinsPerQuestion(dto.TotalQuestions);
        int coinsEarned = newlyCorrect.Count * coinsPerQ;

        // 3. Бонус x1.5 за идеал с первой попытки
        bool idealBonus = isFirstAttempt && dto.Score == 100;
        if (idealBonus)
            coinsEarned = (int)(coinsEarned * 1.5);

        // 4. XP — только за новый статус
        int currentStatus = CalcStatus(dto.Score, dto.TotalQuestions);
        int prevStatus = testStats.BestStatus;
        int xpEarned = CalcXpForNewStatus(prevStatus, currentStatus, dto.TotalQuestions);
        bool isNewStatus = currentStatus > prevStatus;

        if (idealBonus && currentStatus == 4)
        {
            int[] idealXpBonus = dto.TotalQuestions switch
            {
                5  => [5],
                10 => [10],
                20 => [20],
                _  => [30]
            };
            xpEarned += idealXpBonus[0];
        }

        if (isNewStatus)
            testStats.BestStatus = currentStatus;

        await statsRepo.SaveTestStatsAsync(testStats);

        // 5. Сохраняем попытку
        await statsRepo.AddTestAttemptAsync(new TestAttempt
        {
            UserId = userId,
            ParagraphId = dto.ParagraphId,
            TestId = dto.TestId,
            Score = dto.Score,
            CorrectAnswers = dto.CorrectAnswers,
            TotalQuestions = dto.TotalQuestions,
            WrongQuestionIds = JsonSerializer.Serialize(dto.WrongQuestionIds ?? []),
            CorrectQuestionIds = JsonSerializer.Serialize(dto.CorrectQuestionIds ?? []),
            TimeSpent = dto.TimeSpent
        });

        // 6. Начисляем награды
        if (coinsEarned > 0) await gamification.AddCoinsAsync(userId, coinsEarned);
        if (xpEarned > 0)    await gamification.AddXpAsync(userId, xpEarned);
        await gamification.UpdateStreakAsync(userId);

        // 6b. Обновляем SR-интервал на основе среднего усвоения по всем тестам параграфа
        var allParaTests = await statsRepo.GetTestStatsByParagraphAsync(userId, dto.ParagraphId);
        // Включаем только тесты с хотя бы одной попыткой (включая только что сохранённый)
        var attemptedTests = allParaTests.Where(t => t.AttemptsCount > 0).ToList();
        int mastery = attemptedTests.Count > 0
            ? (int)Math.Round(attemptedTests.Average(t => t.BestScore))
            : dto.Score;

        var readingStats = await statsRepo.GetReadingStatsAsync(userId, dto.ParagraphId)
            ?? new ParagraphReadingStats { UserId = userId, ParagraphId = dto.ParagraphId };
        bool hasWrong = (dto.WrongQuestionIds?.Count ?? 0) > 0;
        readingStats = ApplyMasteryInterval(readingStats, mastery, hasWrong);
        await statsRepo.SaveReadingStatsAsync(readingStats);

        // 7. Лог
        await statsRepo.AddActivityLogAsync(new ActivityLog
        {
            UserId = userId,
            ActionType = "test_complete",
            ParagraphId = dto.ParagraphId,
            TestId = dto.TestId,
            XpEarned = xpEarned,
            CoinsEarned = coinsEarned,
            TimeSpent = dto.TimeSpent
        });

        return new TestRewardResponse(coinsEarned, xpEarned, StatusName(currentStatus), isNewStatus, idealBonus);
    }

    public async Task CompleteReadingAsync(int userId, ReadingSessionRequest dto)
    {
        var stats = await statsRepo.GetReadingStatsAsync(userId, dto.ParagraphId)
            ?? new ParagraphReadingStats { UserId = userId, ParagraphId = dto.ParagraphId };

        stats.TotalReadingSeconds += dto.TimeSpent;
        stats.ReadingSessionsCount++;
        if (dto.CodeWasRun) stats.CodeRunsCount++;
        if (dto.ScrollPixels > 0) stats.ScrollPixels += dto.ScrollPixels;
        stats.LastReadAt = DateTime.UtcNow;

        // Чтение устанавливает SR только если тест ещё не проходился
        // (не перезаписываем интервал, выставленный по результату теста)
        if (stats.SrRepetitions == 0 && stats.SrNextDue == null)
        {
            stats.SrNextDue = DateTime.UtcNow.Date.AddDays(1);
            stats.SrInterval = 1;
        }

        await statsRepo.SaveReadingStatsAsync(stats);

        await gamification.AddXpAsync(userId, ReadXp);
        await gamification.AddCoinsAsync(userId, ReadCoins);
        await gamification.UpdateStreakAsync(userId);

        await statsRepo.AddActivityLogAsync(new ActivityLog
        {
            UserId = userId,
            ActionType = "read_paragraph",
            ParagraphId = dto.ParagraphId,
            XpEarned = ReadXp,
            CoinsEarned = ReadCoins,
            TimeSpent = dto.TimeSpent
        });
    }

    // Mastery-based interval: чем ниже усвоение — тем короче интервал.
    // mastery = среднее арифметическое bestScore по всем тестам параграфа (0–100).
    // Логика: если есть неправильные вопросы → повтор завтра.
    // Если всё правильно, интервал растёт с каждым успешным повторением.
    private static ParagraphReadingStats ApplyMasteryInterval(ParagraphReadingStats s, int mastery, bool hasWrongQuestions)
    {
        int interval;

        if (hasWrongQuestions || mastery < 70)
        {
            // Есть ошибки или не прошёл порог → повтор завтра, сбрасываем серию
            interval = 1;
            s.SrRepetitions = 0;
        }
        else
        {
            // Прошёл тест — интервал растёт с каждым успешным повторением
            // Базовый интервал зависит от результата, затем умножается на серию
            int baseInterval = mastery switch
            {
                < 80  => 3,   // зачёт (70–79%) → 3 дня
                < 90  => 5,   // бронза (80–89%) → 5 дней
                < 100 => 7,   // серебро (90–99%) → неделя
                _     => 10   // золото (100%) → 10 дней
            };

            // Множитель за серию успешных повторений (не более x3)
            float multiplier = Math.Min(3.0f, 1.0f + s.SrRepetitions * 0.5f);
            interval = (int)Math.Round(baseInterval * multiplier);
            interval = Math.Min(interval, 30); // максимум 30 дней
            s.SrRepetitions++;
        }

        s.SrInterval = interval;
        s.SrNextDue = DateTime.UtcNow.Date.AddDays(interval);
        s.SrEaseFactor = 2.5f;
        return s;
    }

    // SM-2 оставляем для обратной совместимости (больше не вызывается)
    private static ParagraphReadingStats ApplySm2(ParagraphReadingStats s, int q)
    {
        if (s.SrInterval > 30) { s.SrInterval = 1; s.SrRepetitions = 0; }
        if (q >= 3)
        {
            s.SrInterval = s.SrRepetitions switch { 0 => 1, 1 => 3, _ => (int)Math.Round(s.SrInterval * s.SrEaseFactor) };
            s.SrRepetitions++;
        }
        else { s.SrRepetitions = 0; s.SrInterval = 1; }
        s.SrEaseFactor = Math.Max(1.3f, s.SrEaseFactor + 0.1f - (5 - q) * (0.08f + (5 - q) * 0.02f));
        s.SrInterval = Math.Min(s.SrInterval, 30);
        s.SrNextDue = DateTime.UtcNow.Date.AddDays(s.SrInterval);
        return s;
    }

    public async Task<Dictionary<string, ParagraphProgressResponse>> GetAllProgressAsync(int userId)
    {
        var allReading = await statsRepo.GetAllReadingStatsAsync(userId);
        var allTests   = await statsRepo.GetAllTestStatsAsync(userId);

        // Группируем тесты по paragraphId
        var testsByParagraph = allTests.GroupBy(t => t.ParagraphId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var readingByParagraph = allReading.ToDictionary(r => r.ParagraphId);

        // Собираем все уникальные paragraphId
        var allParagraphIds = readingByParagraph.Keys
            .Union(testsByParagraph.Keys)
            .ToHashSet();

        var result = new Dictionary<string, ParagraphProgressResponse>();

        foreach (var paragraphId in allParagraphIds)
        {
            var reading = readingByParagraph.GetValueOrDefault(paragraphId);
            var testStatsList = testsByParagraph.GetValueOrDefault(paragraphId) ?? [];

            var tests = new Dictionary<string, TestStatsDto>();
            foreach (var ts in testStatsList)
            {
                var lastAttempt = await statsRepo.GetLastAttemptAsync(userId, ts.TestId);
                var wrongIds = lastAttempt is not null
                    ? JsonSerializer.Deserialize<List<int>>(lastAttempt.WrongQuestionIds) ?? []
                    : [];

                var quizMeta = await quizService.GetQuizAsync(ts.TestId);
                var bankSize = quizMeta?.Questions.Count ?? 0;
                var bankIds  = quizMeta?.Questions.Select(q => q.Id).ToHashSet() ?? [];

                var qProgress = await questionProgressRepo.GetByTestAsync(userId, ts.TestId);
                var currentlyCorrect = qProgress.Count(q => q.IsCorrect && bankIds.Contains(q.QuestionId));

                tests[ts.TestId] = new TestStatsDto(
                ts.TestTitle,
                ts.AttemptsCount,
                ts.BestScore,
                ts.AttemptsCount > 0 ? ts.TotalScoreSum / ts.AttemptsCount : 0,
                ts.LastScore,
                wrongIds,
                ts.LastAttemptAt,
                currentlyCorrect,
                bankSize
            );
            }

            var totalSec = reading?.TotalReadingSeconds ?? 0;
            var sessions = reading?.ReadingSessionsCount ?? 0;

            result[paragraphId] = new ParagraphProgressResponse(
                paragraphId,
                totalSec,
                sessions,
                sessions > 0 ? totalSec / sessions : 0,
                reading?.CodeRunsCount ?? 0,
                reading?.LastReadAt,
                reading?.SrNextDue,
                reading?.SrRepetitions ?? 0,
                tests
            );
        }

        return result;
    }

    public async Task<ParagraphProgressResponse> GetParagraphProgressAsync(int userId, string paragraphId)
    {
        var reading = await statsRepo.GetReadingStatsAsync(userId, paragraphId);
        var testStatsList = await statsRepo.GetTestStatsByParagraphAsync(userId, paragraphId);

        var tests = new Dictionary<string, TestStatsDto>();
        foreach (var ts in testStatsList)
        {
            var lastAttempt = await statsRepo.GetLastAttemptAsync(userId, ts.TestId);
            var wrongIds = lastAttempt is not null
                ? JsonSerializer.Deserialize<List<int>>(lastAttempt.WrongQuestionIds) ?? []
                : [];

            var quizMeta = await quizService.GetQuizAsync(ts.TestId);
            var bankSize = quizMeta?.Questions.Count ?? 0;
            var bankIds  = quizMeta?.Questions.Select(q => q.Id).ToHashSet() ?? [];

            var qProgress = await questionProgressRepo.GetByTestAsync(userId, ts.TestId);
            var currentlyCorrect = qProgress.Count(q => q.IsCorrect && bankIds.Contains(q.QuestionId));

            tests[ts.TestId] = new TestStatsDto(
                ts.TestTitle,
                ts.AttemptsCount,
                ts.BestScore,
                ts.AttemptsCount > 0 ? ts.TotalScoreSum / ts.AttemptsCount : 0,
                ts.LastScore,
                wrongIds,
                ts.LastAttemptAt,
                currentlyCorrect,
                bankSize
            );
        }

        var totalSec = reading?.TotalReadingSeconds ?? 0;
        var sessions = reading?.ReadingSessionsCount ?? 0;

        return new ParagraphProgressResponse(
            paragraphId,
            totalSec,
            sessions,
            sessions > 0 ? totalSec / sessions : 0,
            reading?.CodeRunsCount ?? 0,
            reading?.LastReadAt,
            reading?.SrNextDue,
            reading?.SrRepetitions ?? 0,
            tests
        );
    }
}
