namespace CppQuest.Gateway.Configuration;

/// <summary>
/// Настройки политики CORS (Cross-Origin Resource Sharing).
/// Список доверенных доменов поддерживает Hot Reload —
/// изменения в <c>appsettings.json</c> применяются без перезапуска.
/// </summary>
public record CorsConfig
{
    /// <summary>
    /// Список доверенных Origin-доменов, которым разрешены кросс-доменные запросы.
    /// Preflight OPTIONS-запросы от этих доменов получат статус 204 с CORS-заголовками.
    /// Запросы от доменов вне списка вернут ответ без CORS-заголовков.
    /// Пример: <c>["https://cppquest.ru", "https://www.cppquest.ru"]</c>
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];
}
