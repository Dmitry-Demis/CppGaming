namespace cppQuest.Server.DTOs;

public record QuizResponse(
    string QuizId,
    string Title,
    string Type,
    int PassingScore,
    int Pick,
    List<QuizQuestionDto> Questions
);

public record QuizQuestionDto(
    int Id,
    string Type,
    string Question,
    string? Code,
    List<string>? Answers,
    object? Correct,
    string? Explanation,
    string? Std = null,
    List<QuizPairDto>? Pairs = null,
    List<string>? Items = null,
    List<string>? Categories = null
);

public record QuizPairDto(string Left, string Right);

public record QuizSubmitRequest(Dictionary<string, object> Answers);

public record QuizSubmitResponse(
    int Pct,
    bool Passed,
    int Earned,
    int MaxScore,
    List<QuizResultDto> Results
);

public record QuizResultDto(
    int Id,
    bool IsRight,
    int Pts,
    string Explanation,
    object Correct
);
