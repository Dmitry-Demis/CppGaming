using cppQuest.Server.DTOs;

namespace cppQuest.Server.Services;

public interface IQuizService
{
    Task<QuizResponse?> GetQuizAsync(string quizId);
    Task<QuizSubmitResponse?> SubmitQuizAsync(string quizId, QuizSubmitRequest request);
}
