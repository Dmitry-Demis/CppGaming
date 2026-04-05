using System.Text.Json;
using cppQuest.Server.DTOs;
using cppQuest.Server.Models;
using cppQuest.Server.Repositories;

namespace cppQuest.Server.Services;

/// <summary>
/// Отвечает за сохранение статистики тестов и чтения.
/// Расчёт наград делегирует в <see cref="TestRewardCalculator"/>,
/// SR-интервалы — в <see cref="SpacedRepetitionService"/>,
/// начисление XP/монет/стрика — в <see cref="GamificationService"/>.
/// </summary>
public class StatsService(
    IStatsRepository statsRepo,
    GamificationService gamification,
    IQuizService quizService,
    IQuestionProgressRepository questionProgressRepo)
{
    private const int ReadXp    = 15;
    private const int ReadCoins = 5;

    // ── Тест ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Фиксирует результат теста, обновляет статистику и начисляет награды.
    ///
    /// Порядок операций:
    ///   1. Обновить агрегированную статистику теста (ParagraphTestStats).
    ///   2. Рассчитать монеты за новые правильные вопросы.
    ///   3. Рассчитать XP за новый статус.
    ///   4. Сохранить попытку (TestAttempt).
    ///   5. Начислить монеты, XP, обновить стрик.
    ///   6. Обновить SR-интервал параграфа на основе среднего mastery.
    ///   7. Записать лог активности.
    /// </summary>
    public async Task<TestRewardResponse> CompleteTestAsync(int userId, TestCompleteRequest dto)
    {
        // 1. Агрегированная статистика
        var testStats = await statsRepo.GetTestStatsAsync(userId, dto.TestId)
            ?? new ParagraphTestStats { UserId = userId, TestId = dto.TestId, ParagraphId = dto.ParagraphId };

        bool isFirstAttempt = testStats.AttemptsCount == 0;
        testStats.AttemptsCount++;
        testStats.TotalScoreSum += dto.Score;
        testStats.LastScore      = dto.Score;
        if (dto.Score > testStats.BestScore) testStats.BestScore = dto.Score;
        testStats.LastAttemptAt  = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dto.TestTitle) && string.IsNullOrEmpty(testStats.TestTitle))
            testStats.TestTitle = dto.TestTitle;

        // 2. Монеты — только за вопросы, правильно отвеченные ВПЕРВЫЕ за всё время
        //    everCorrect загружаем до сохранения попытки, чтобы не считать текущую
        var everCorrect  = await statsRepo.GetEverCorrectQuestionIdsAsync(userId, dto.TestId);
        int newlyCorrect = (dto.CorrectQuestionIds ?? []).Count(id => !everCorrect.Contains(id));
        bool idealBonus  = isFirstAttempt && dto.Score == 100;
        int coinsEarned  = TestRewardCalculator.CalcCoins(newlyCorrect, dto.TotalQuestions, idealBonus);

        // 3. XP за статус — только за новый (лучший) статус
        int currentStatus = TestRewardCalculator.CalcStatus(dto.Score, dto.TotalQuestions);
        int prevStatus    = testStats.BestStatus;
        int xpEarned      = TestRewardCalculator.CalcXpForNewStatus(prevStatus, currentStatus, dto.TotalQuestions);
        bool isNewStatus  = currentStatus > prevStatus;

        // Идеальный бонус x1.5 применяется к XP за статус
        if (idealBonus && xpEarned > 0)
            xpEarned = TestRewardCalculator.ApplyIdealXpBonus(xpEarned);

        if (isNewStatus) testStats.BestStatus = currentStatus;

        // 4. XP за освоение банка вопросов
        var quizMeta    = await quizService.GetQuizAsync(dto.TestId);
        int bankSize    = quizMeta?.Questions.Count ?? dto.TotalQuestions;

        // everCorrect уже загружен выше — добавляем правильные из текущей попытки
        var allEverCorrect = everCorrect.Union(dto.CorrectQuestionIds ?? []).ToHashSet();
        int everCorrectCount = allEverCorrect.Count;

        int bankMasteryXp = TestRewardCalculator.CalcBankMasteryXp(
            everCorrectCount, bankSize,
            testStats.BankMasteryXpAwarded,
            out int newMask);

        testStats.BankMasteryXpAwarded = newMask;
        xpEarned += bankMasteryXp;

        await statsRepo.SaveTestStatsAsync(testStats);

        // 5. Попытка
        await statsRepo.AddTestAttemptAsync(new TestAttempt
        {
            UserId             = userId,
            ParagraphId        = dto.ParagraphId,
            TestId             = dto.TestId,
            Score              = dto.Score,
            CorrectAnswers     = dto.CorrectAnswers,
            TotalQuestions     = dto.TotalQuestions,
            WrongQuestionIds   = JsonSerializer.Serialize(dto.WrongQuestionIds   ?? []),
            CorrectQuestionIds = JsonSerializer.Serialize(dto.CorrectQuestionIds ?? []),
            TimeSpent          = dto.TimeSpent
        });

        // 6. Начисляем награды
        if (coinsEarned > 0) await gamification.AddCoinsAsync(userId, coinsEarned);
        if (xpEarned    > 0) await gamification.AddXpAsync(userId, xpEarned);
        await gamification.UpdateStreakAsync(userId);

        // 7. SR-интервал параграфа
        var allParaTests   = await statsRepo.GetTestStatsByParagraphAsync(userId, dto.ParagraphId);
        var attemptedTests = allParaTests.Where(t => t.AttemptsCount > 0).ToList();
        int mastery        = attemptedTests.Count > 0
            ? (int)Math.Round(attemptedTests.Average(t => t.BestScore))
            : dto.Score;

        var readingStats = await statsRepo.GetReadingStatsAsync(userId, dto.ParagraphId)
            ?? new ParagraphReadingStats { UserId = userId, ParagraphId = dto.ParagraphId };
        bool hasWrong = (dto.WrongQuestionIds?.Count ?? 0) > 0;
        SpacedRepetitionService.ApplyMasteryInterval(readingStats, mastery, hasWrong);
        await statsRepo.SaveReadingStatsAsync(readingStats);

        // 8. Лог
        await statsRepo.AddActivityLogAsync(new ActivityLog
        {
            UserId      = userId,
            ActionType  = "test_complete",
            ParagraphId = dto.ParagraphId,
            TestId      = dto.TestId,
            XpEarned    = xpEarned,
            CoinsEarned = coinsEarned,
            TimeSpent   = dto.TimeSpent
        });

        return new TestRewardResponse(coinsEarned, xpEarned,
            TestRewardCalculator.StatusName(currentStatus), isNewStatus, idealBonus);
    }

    // ── Чтение ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Фиксирует сессию чтения параграфа.
    /// SR-интервал устанавливается только если тест ещё не проходился
    /// (чтобы не перезаписывать интервал, выставленный по результату теста).
    /// </summary>
    public async Task CompleteReadingAsync(int userId, ReadingSessionRequest dto)
    {
        var stats = await statsRepo.GetReadingStatsAsync(userId, dto.ParagraphId)
            ?? new ParagraphReadingStats { UserId = userId, ParagraphId = dto.ParagraphId };

        stats.TotalReadingSeconds  += dto.TimeSpent;
        stats.ReadingSessionsCount++;
        if (dto.CodeWasRun)        stats.CodeRunsCount++;
        if (dto.ScrollPixels > 0)  stats.ScrollPixels += dto.ScrollPixels;
        stats.LastReadAt = DateTime.UtcNow;

        if (stats.SrRepetitions == 0 && stats.SrNextDue == null)
        {
            stats.SrNextDue  = DateTime.UtcNow.Date.AddDays(1);
            stats.SrInterval = 1;
        }

        await statsRepo.SaveReadingStatsAsync(stats);
        await gamification.AddXpAsync(userId, ReadXp);
        await gamification.AddCoinsAsync(userId, ReadCoins);
        await gamification.UpdateStreakAsync(userId);

        await statsRepo.AddActivityLogAsync(new ActivityLog
        {
            UserId      = userId,
            ActionType  = "read_paragraph",
            ParagraphId = dto.ParagraphId,
            XpEarned    = ReadXp,
            CoinsEarned = ReadCoins,
            TimeSpent   = dto.TimeSpent
        });
    }

    // ── Прогресс ─────────────────────────────────────────────────────────────

    public async Task<Dictionary<string, ParagraphProgressResponse>> GetAllProgressAsync(int userId)
    {
        var allReading = await statsRepo.GetAllReadingStatsAsync(userId);
        var allTests   = await statsRepo.GetAllTestStatsAsync(userId);

        var testsByParagraph   = allTests.GroupBy(t => t.ParagraphId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var readingByParagraph = allReading.ToDictionary(r => r.ParagraphId);

        var allParagraphIds = readingByParagraph.Keys.Union(testsByParagraph.Keys).ToHashSet();
        var result = new Dictionary<string, ParagraphProgressResponse>();

        foreach (var paragraphId in allParagraphIds)
        {
            var reading      = readingByParagraph.GetValueOrDefault(paragraphId);
            var testStatsList = testsByParagraph.GetValueOrDefault(paragraphId) ?? [];
            var tests        = await BuildTestStatsDictAsync(userId, testStatsList);
            result[paragraphId] = BuildProgressResponse(paragraphId, reading, tests);
        }

        return result;
    }

    public async Task<ParagraphProgressResponse> GetParagraphProgressAsync(int userId, string paragraphId)
    {
        var reading      = await statsRepo.GetReadingStatsAsync(userId, paragraphId);
        var testStatsList = await statsRepo.GetTestStatsByParagraphAsync(userId, paragraphId);
        var tests        = await BuildTestStatsDictAsync(userId, testStatsList);
        return BuildProgressResponse(paragraphId, reading, tests);
    }

    // ── Вспомогательные ──────────────────────────────────────────────────────

    private async Task<Dictionary<string, TestStatsDto>> BuildTestStatsDictAsync(
        int userId, IEnumerable<ParagraphTestStats> testStatsList)
    {
        var result = new Dictionary<string, TestStatsDto>();
        foreach (var ts in testStatsList)
        {
            var lastAttempt = await statsRepo.GetLastAttemptAsync(userId, ts.TestId);
            var wrongIds    = lastAttempt is not null
                ? JsonSerializer.Deserialize<List<int>>(lastAttempt.WrongQuestionIds) ?? []
                : [];

            var quizMeta         = await quizService.GetQuizAsync(ts.TestId);
            var bankIds          = quizMeta?.Questions.Select(q => q.Id).ToHashSet() ?? [];
            var qProgress        = await questionProgressRepo.GetByTestAsync(userId, ts.TestId);
            int currentlyCorrect = qProgress.Count(q => q.IsCorrect && bankIds.Contains(q.QuestionId));

            result[ts.TestId] = new TestStatsDto(
                ts.TestTitle,
                ts.AttemptsCount,
                ts.BestScore,
                ts.AttemptsCount > 0 ? ts.TotalScoreSum / ts.AttemptsCount : 0,
                ts.LastScore,
                wrongIds,
                ts.LastAttemptAt,
                currentlyCorrect,
                bankIds.Count
            );
        }
        return result;
    }

    private static ParagraphProgressResponse BuildProgressResponse(
        string paragraphId,
        ParagraphReadingStats? reading,
        Dictionary<string, TestStatsDto> tests)
    {
        int totalSec = reading?.TotalReadingSeconds ?? 0;
        int sessions = reading?.ReadingSessionsCount ?? 0;
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
