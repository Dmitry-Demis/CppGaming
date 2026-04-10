namespace CppQuest.Gateway.Configuration;

/// <summary>
/// Корневая конфигурация API Gateway.
/// Объединяет все секции настроек, загружаемых из <c>appsettings.json</c>.
/// </summary>
public record GatewayConfiguration
{
    /// <summary>
    /// Настройки ограничения частоты запросов (Rate Limiting).
    /// </summary>
    public RateLimitingConfig RateLimiting { get; init; } = new();

    /// <summary>
    /// Настройки валидации JWT-токенов.
    /// </summary>
    public JwtConfig Jwt { get; init; } = new();

    /// <summary>
    /// Настройки политики CORS (список доверенных доменов).
    /// </summary>
    public CorsConfig Cors { get; init; } = new();

    /// <summary>
    /// Настройки подключения к Kafka для публикации событий аудита.
    /// </summary>
    public KafkaConfig Kafka { get; init; } = new();
}
