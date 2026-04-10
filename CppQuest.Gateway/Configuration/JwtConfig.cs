namespace CppQuest.Gateway.Configuration;

/// <summary>
/// Настройки валидации JWT-токенов.
/// Публичный RSA-ключ загружается один раз при старте приложения —
/// без обращения к Auth_Service при каждом запросе.
/// </summary>
public record JwtConfig
{
    /// <summary>
    /// Путь к файлу публичного RSA-ключа в формате PEM,
    /// используемого для проверки подписи JWT.
    /// Пример: <c>keys/public.pem</c>
    /// </summary>
    public string PublicKeyPath { get; init; } = string.Empty;

    /// <summary>
    /// Ожидаемое значение claim <c>iss</c> (издатель токена).
    /// Токены с другим issuer будут отклонены.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Ожидаемое значение claim <c>aud</c> (аудитория токена).
    /// Токены с другой аудиторией будут отклонены.
    /// </summary>
    public string Audience { get; init; } = string.Empty;
}
