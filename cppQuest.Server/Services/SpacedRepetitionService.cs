using cppQuest.Server.Models;

namespace cppQuest.Server.Services;

/// <summary>
/// Чистая логика Spaced Repetition (SR) для параграфов.
/// Не имеет зависимостей — только вычисления над моделью.
///
/// Алгоритм ApplyMasteryInterval:
///   mastery = среднее арифметическое BestScore по всем тестам параграфа (0–100).
///
///   Если есть неправильные вопросы ИЛИ mastery < 70:
///     → interval = 1 день, SrRepetitions сбрасывается в 0.
///
///   Иначе (тест пройден):
///     baseInterval зависит от mastery:
///       70–79% → 3 дня   (зачёт)
///       80–89% → 5 дней  (бронза)
///       90–99% → 7 дней  (серебро)
///       100%   → 10 дней (золото)
///
///     multiplier = min(3.0, 1.0 + SrRepetitions × 0.5)
///       — растёт с каждым успешным повторением, но не более ×3.
///
///     interval = round(baseInterval × multiplier), но не более 30 дней.
///     SrRepetitions++.
///
/// Алгоритм NextQuestionInterval (для отдельных вопросов):
///   Правильно → min(currentInterval × 2, 30)
///   Неправильно → 1
/// </summary>
public static class SpacedRepetitionService
{
    public const int MaxInterval = 30;

    /// <summary>
    /// Применяет SR-интервал к статистике чтения параграфа на основе mastery.
    /// Мутирует переданный объект и возвращает его же.
    /// </summary>
    public static ParagraphReadingStats ApplyMasteryInterval(
        ParagraphReadingStats stats, int mastery, bool hasWrongQuestions)
    {
        int interval;

        if (hasWrongQuestions || mastery < 70)
        {
            interval           = 1;
            stats.SrRepetitions = 0;
        }
        else
        {
            int baseInterval = mastery switch
            {
                < 80  => 3,
                < 90  => 5,
                < 100 => 7,
                _     => 10
            };

            float multiplier = Math.Min(3.0f, 1.0f + stats.SrRepetitions * 0.5f);
            interval = Math.Min((int)Math.Round(baseInterval * multiplier), MaxInterval);
            stats.SrRepetitions++;
        }

        stats.SrInterval = interval;
        stats.SrNextDue  = DateTime.UtcNow.Date.AddDays(interval);
        stats.SrEaseFactor = 2.5f;
        return stats;
    }

    /// <summary>
    /// Следующий интервал для отдельного вопроса (используется в QuestionProgressService).
    /// </summary>
    public static int NextQuestionInterval(int currentInterval, bool wasCorrect) =>
        wasCorrect ? Math.Min(currentInterval * 2, MaxInterval) : 1;
}
