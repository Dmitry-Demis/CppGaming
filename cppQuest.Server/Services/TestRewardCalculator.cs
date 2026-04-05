namespace cppQuest.Server.Services;

/// <summary>
/// Логика расчёта наград за тест.
///
/// ── Статусы (0–4) ────────────────────────────────────────────────────────────
///   0 = failed, 1 = passed, 2 = bronze, 3 = silver, 4 = gold
///   Мини-тест (5 вопросов): серебра нет, сразу gold при 100%.
///   Остальные: 100% = gold, ≥90% = silver, ≥80% = bronze, ≥70% = passed, иначе failed.
///
/// ── Монеты ───────────────────────────────────────────────────────────────────
///   Даются за каждый правильный ответ в текущей попытке.
///   Ставка зависит от размера теста: 5→10, 10→15, 20→20, 40+→25 монет/вопрос.
///   Бонус x1.5 (монеты И XP) если: первая попытка теста И результат 100%.
///
/// ── XP за статус ─────────────────────────────────────────────────────────────
///   Начисляется только за НОВЫЙ статус (лучше предыдущего лучшего).
///   Таблица накопленного XP:
///                  failed | passed | bronze | silver | gold | ideal
///     5 вопросов:  [0, 10, 13, 13, 25]
///     10 вопросов: [0, 15, 22, 32, 45]
///     20 вопросов: [0, 25, 40, 60, 83]
///     40+ вопросов:[0, 35, 55, 80, 110]
///   XP = table[newStatus] - table[prevStatus].
///
/// ── XP за освоение банка вопросов ────────────────────────────────────────────
///   Банк — все вопросы теста (может расти со временем).
///   Освоение = EverCorrectCount / BankSize * 100%.
///   Пороги: 70%, 80%, 90%, 100% — каждый даёт XP один раз за всё время.
///   Достигнутые пороги хранятся в ParagraphTestStats.BankMasteryXpAwarded (битовая маска).
///   Если банк вырос и освоение упало ниже порога — повторно XP не даётся.
///   Маска: бит 0 = 70%, бит 1 = 80%, бит 2 = 90%, бит 3 = 100%.
/// </summary>
public static class TestRewardCalculator
{
    // ── Статусы ──────────────────────────────────────────────────────────────

    public static int CalcStatus(int pct, int totalQuestions) =>
        totalQuestions switch
        {
            5 => pct switch { 100 => 4, >= 80 => 2, >= 60 => 1, _ => 0 },
            _ => pct switch { 100 => 4, >= 90 => 3, >= 80 => 2, >= 70 => 1, _ => 0 }
        };

    public static string StatusName(int status) => status switch
    {
        4 => "gold", 3 => "silver", 2 => "bronze", 1 => "passed", _ => "failed"
    };

    // ── XP за статус ─────────────────────────────────────────────────────────

    /// <summary>
    /// XP за достижение нового статуса (разница накопленных значений).
    /// Возвращает 0, если новый статус не лучше предыдущего.
    /// </summary>
    public static int CalcXpForNewStatus(int prevStatus, int newStatus, int totalQuestions)
    {
        if (newStatus <= prevStatus) return 0;

        int[] table = totalQuestions switch
        {
            5  => [0, 10, 13, 13, 25],
            10 => [0, 15, 22, 32, 45],
            20 => [0, 25, 40, 60, 83],
            _  => [0, 35, 55, 80, 110]
        };

        return table[newStatus] - table[prevStatus];
    }

    // ── Идеальный бонус ───────────────────────────────────────────────────────

    /// <summary>
    /// Бонус XP за идеальный результат (100%) с первой попытки.
    /// Применяется к XP за статус: итоговый XP = round(xp * 1.5).
    /// </summary>
    public static int ApplyIdealXpBonus(int xp) => (int)Math.Round(xp * 1.5);

    /// <summary>
    /// Бонус монет за идеальный результат (100%) с первой попытки.
    /// Применяется к монетам за попытку: итог = round(coins * 1.5).
    /// </summary>
    public static int ApplyIdealCoinsBonus(int coins) => (int)Math.Round(coins * 1.5);

    // ── Монеты ───────────────────────────────────────────────────────────────

    /// <summary>Монет за один правильный ответ в зависимости от размера теста.</summary>
    public static int CoinsPerQuestion(int totalQuestions) => totalQuestions switch
    {
        5  => 10,
        10 => 15,
        20 => 20,
        _  => 25
    };

    /// <summary>
    /// Монеты за попытку: newlyCorrectCount * ставка, с опциональным бонусом x1.5.
    /// newlyCorrectCount — количество вопросов, правильно отвеченных ВПЕРВЫЕ за всё время.
    /// Повторное прохождение уже решённых вопросов монет не даёт.
    /// </summary>
    public static int CalcCoins(int newlyCorrectCount, int totalQuestions, bool idealBonus)
    {
        int coins = newlyCorrectCount * CoinsPerQuestion(totalQuestions);
        return idealBonus ? ApplyIdealCoinsBonus(coins) : coins;
    }

    // ── XP за освоение банка ─────────────────────────────────────────────────

    /// <summary>
    /// Пороги освоения банка и соответствующий XP.
    /// Индекс в массиве = номер бита в BankMasteryXpAwarded.
    /// </summary>
    private static readonly (int ThresholdPct, int Xp)[] BankMasteryThresholds =
    [
        (70,  20),   // бит 0: 70% банка освоено → +20 XP
        (80,  35),   // бит 1: 80% банка освоено → +35 XP
        (90,  55),   // бит 2: 90% банка освоено → +55 XP
        (100, 80),   // бит 3: 100% банка освоено → +80 XP
    ];

    /// <summary>
    /// Рассчитывает XP за новые достигнутые пороги освоения банка.
    ///
    /// Алгоритм:
    ///   1. Считаем текущий процент освоения: everCorrectCount / bankSize * 100.
    ///   2. Для каждого порога проверяем: достигнут ли он И не был ли уже выдан XP (бит в маске).
    ///   3. Если порог новый — добавляем XP, ставим бит в маске.
    ///   4. Возвращаем суммарный XP и обновлённую маску.
    ///
    /// Если bankSize = 0 — возвращаем 0 (нет вопросов, нет наград).
    /// Если банк вырос и освоение упало — биты уже стоят, повторно не выдаём.
    /// </summary>
    /// <param name="everCorrectCount">Сколько уникальных вопросов из банка решено верно за всё время.</param>
    /// <param name="bankSize">Текущий размер банка вопросов.</param>
    /// <param name="awardedMask">Текущая битовая маска выданных порогов (из БД).</param>
    /// <param name="newMask">Обновлённая маска после начисления.</param>
    /// <returns>XP к начислению (0 если новых порогов нет).</returns>
    public static int CalcBankMasteryXp(
        int everCorrectCount, int bankSize, int awardedMask, out int newMask)
    {
        newMask = awardedMask;
        if (bankSize <= 0) return 0;

        int masteryPct = everCorrectCount * 100 / bankSize;
        int totalXp    = 0;

        for (int i = 0; i < BankMasteryThresholds.Length; i++)
        {
            int bit = 1 << i;
            var (threshold, xp) = BankMasteryThresholds[i];

            bool alreadyAwarded = (awardedMask & bit) != 0;
            if (!alreadyAwarded && masteryPct >= threshold)
            {
                totalXp  += xp;
                newMask  |= bit;
            }
        }

        return totalXp;
    }
}
