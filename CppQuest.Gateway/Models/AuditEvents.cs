namespace CppQuest.Gateway.Models;

/// <summary>
/// Событие успешного доступа к API.
/// Публикуется в топик Kafka <c>gateway.access.events</c>
/// после каждого успешно проксированного запроса.
/// </summary>
public record AccessAuditEvent
{
    /// <summary>
    /// Момент времени, когда запрос был обработан Gateway.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// IP-адрес клиента, отправившего запрос.
    /// </summary>
    public string ClientIp { get; init; } = string.Empty;

    /// <summary>
    /// Идентификатор пользователя из claim <c>sub</c> JWT-токена.
    /// Равен <c>null</c> для анонимных запросов на публичные маршруты.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Идентификатор маршрута YARP, на который был направлен запрос.
    /// Пример: <c>auth-route</c>, <c>profile-route</c>, <c>legacy-fallback</c>
    /// </summary>
    public string Route { get; init; } = string.Empty;

    /// <summary>
    /// HTTP-метод запроса в верхнем регистре.
    /// Пример: <c>GET</c>, <c>POST</c>, <c>PUT</c>
    /// </summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>
    /// HTTP-статус код ответа, полученного от внутреннего сервиса.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Время обработки запроса в миллисекундах,
    /// измеренное на уровне Gateway (без учёта сетевых задержек клиента).
    /// </summary>
    public long DurationMs { get; init; }
}

/// <summary>
/// Событие нарушения безопасности.
/// Публикуется в топик Kafka <c>gateway.security.events</c>
/// при неудачной JWT-валидации (отсутствие токена, неверная подпись, истёкший срок).
/// </summary>
public record SecurityAuditEvent
{
    /// <summary>
    /// Момент времени, когда была зафиксирована попытка несанкционированного доступа.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// IP-адрес клиента, от которого поступил запрос с невалидным токеном.
    /// </summary>
    public string ClientIp { get; init; } = string.Empty;

    /// <summary>
    /// Причина отказа в валидации JWT.
    /// Возможные значения: <c>TOKEN_MISSING</c>, <c>INVALID_SIGNATURE</c>, <c>TOKEN_EXPIRED</c>.
    /// </summary>
    public string FailureReason { get; init; } = string.Empty;
}
