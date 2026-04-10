namespace CppQuest.Gateway.Configuration;

/// <summary>
/// Настройки ограничения частоты запросов (Rate Limiting).
/// Содержит три политики для разных категорий маршрутов.
/// Счётчики хранятся в Redis для корректной работы
/// при горизонтальном масштабировании Gateway.
/// </summary>
public record RateLimitingConfig
{
    /// <summary>
    /// Политика по умолчанию — применяется ко всем маршрутам,
    /// не попадающим под более специфичные политики.
    /// </summary>
    public RateLimitPolicy DefaultPolicy { get; init; } = new();

    /// <summary>
    /// Политика для аутентифицированных пользователей (с валидным JWT).
    /// Как правило, предоставляет более высокий лимит, чем DefaultPolicy.
    /// </summary>
    public RateLimitPolicy AuthenticatedPolicy { get; init; } = new();

    /// <summary>
    /// Политика для публичных маршрутов (анонимный доступ).
    /// Применяется к маршрутам, явно помеченным как публичные в конфигурации.
    /// </summary>
    public RateLimitPolicy PublicRoutePolicy { get; init; } = new();
}

/// <summary>
/// Параметры одной политики ограничения частоты запросов
/// по алгоритму скользящего окна (Sliding Window).
/// </summary>
public record RateLimitPolicy
{
    /// <summary>
    /// Максимальное количество запросов, разрешённых в течение одного окна.
    /// </summary>
    public int MaxRequests { get; init; }

    /// <summary>
    /// Длина скользящего окна в секундах.
    /// По истечении окна счётчик сбрасывается.
    /// </summary>
    public int WindowSeconds { get; init; }
}
