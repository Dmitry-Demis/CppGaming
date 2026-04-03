namespace cppQuest.Server.DTOs;

// POST /api/reading/complete
public record ReadingSessionRequest(
    string ParagraphId,
    int TimeSpent,       // секунды
    bool CodeWasRun,
    long ScrollPixels = 0  // суммарно прокручено пикселей за сессию
);

// POST /api/test/complete
public record TestCompleteRequest(
    string ParagraphId,
    string TestId,
    string TestTitle,    // название теста для отображения
    int Score,           // процент 0-100
    int CorrectAnswers,
    int TotalQuestions,
    List<int> WrongQuestionIds,
    List<int> CorrectQuestionIds,  // для защиты от фарма монет
    int TimeSpent
);

// Ответ на POST /api/test/complete — реальные начисленные награды
public record TestRewardResponse(
    int CoinsEarned,
    int XpEarned,
    string Status,       // "passed", "bronze", "silver", "gold", "failed"
    bool IsNewStatus,    // повысился ли статус
    bool IdealBonus      // был ли применён бонус x1.5
);

// POST /api/shop/purchase
public record ShopPurchaseRequest(string ItemId);

// POST /api/scroll
public record ScrollRequest(long Pixels);

// POST /api/gated/purchase
public record GatedPurchaseRequest(string Page, string Slot, string Std);

// GET /api/profile/{isuNumber}
public record ProfileResponse(
    int Id,
    Guid PublicId,
    string FirstName,
    string LastName,
    string IsuNumber,
    DateTime RegisteredAt,
    int Xp,
    int Level,
    int Coins,
    int Keys,
    int CurrentStreak,
    int MaxStreak,
    string UnlockedContentStds = "",
    string UnlockedSlots = ""
);

// GET /api/progress/{isuNumber}/{paragraphId}
public record ParagraphProgressResponse(
    string ParagraphId,
    int TotalReadingSeconds,
    int ReadingSessionsCount,
    int AverageReadingSeconds,
    int CodeRunsCount,
    DateTime? LastReadAt,
    DateTime? SrNextDue,
    int SrRepetitions,
    Dictionary<string, TestStatsDto> Tests
);

public record TestStatsDto(
    string TestTitle,
    int AttemptsCount,
    int BestScore,
    int AverageScore,            // вычисляется на сервере
    int LastScore,
    List<int> LastWrongQuestions, // из последней попытки — для повторного показа
    DateTime? LastAttemptAt,
    int SeenCount = 0,           // вопросов, на которые хоть раз ответили правильно (охват изученности)
    int TotalQuestionsInBank = 0 // всего вопросов в банке теста
);

