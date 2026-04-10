namespace CppQuest.Gateway.Models;

/// <summary>
/// Результат проверки лимита запросов для одного ключа (IP или UserID).
/// Возвращается сервисом <c>IRateLimiterService</c> после обращения к Redis.
/// </summary>
/// <param name="IsAllowed">
/// <c>true</c> — запрос разрешён, лимит не превышен;
/// <c>false</c> — лимит превышен, запрос должен быть отклонён со статусом 429.
/// </param>
/// <param name="RetryAfterSeconds">
/// Количество секунд до сброса текущего окна.
/// Используется для заголовка <c>Retry-After</c> в ответе 429.
/// Равен 0, если запрос разрешён.
/// </param>
/// <param name="RemainingRequests">
/// Количество оставшихся разрешённых запросов в текущем окне.
/// Равен 0, если лимит превышен.
/// </param>
public record RateLimitResult(
    bool IsAllowed,
    int RetryAfterSeconds,
    int RemainingRequests
);

/// <summary>
/// Результат валидации JWT-токена.
/// Возвращается сервисом <c>IJwtValidator</c>.
/// При успехе содержит извлечённые claims; при неудаче — причину отказа.
/// </summary>
/// <param name="IsValid">
/// <c>true</c> — токен прошёл все проверки (подпись, срок, issuer, audience);
/// <c>false</c> — токен невалиден, запрос должен быть отклонён со статусом 401.
/// </param>
/// <param name="UserId">
/// Значение claim <c>sub</c> из токена.
/// Равен <c>null</c>, если токен невалиден.
/// </param>
/// <param name="Claims">
/// Все claims из токена в виде словаря <c>имя → значение</c>.
/// Равен <c>null</c>, если токен невалиден.
/// </param>
/// <param name="FailureReason">
/// Причина отказа в валидации.
/// Равен <c>null</c>, если токен валиден.
/// Возможные значения: <c>TOKEN_MISSING</c>, <c>INVALID_SIGNATURE</c>, <c>TOKEN_EXPIRED</c>.
/// </param>
public record JwtValidationResult(
    bool IsValid,
    string? UserId,
    IReadOnlyDictionary<string, string>? Claims,
    string? FailureReason
);
