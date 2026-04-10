namespace CppQuest.Gateway.Configuration;

/// <summary>
/// Настройки подключения к Kafka для асинхронной публикации событий аудита.
/// События буферизируются в памяти через <c>Channel&lt;T&gt;</c> и отправляются
/// фоновым сервисом, не блокируя обработку HTTP-запросов.
/// </summary>
public record KafkaConfig
{
    /// <summary>
    /// Адрес брокера Kafka в формате <c>host:port</c>.
    /// При использовании Docker Compose — имя сервиса из <c>docker-compose.yml</c>.
    /// Пример: <c>kafka:9092</c>
    /// </summary>
    public string BootstrapServers { get; init; } = string.Empty;

    /// <summary>
    /// Имя топика Kafka для событий успешного доступа.
    /// Публикуется после каждого успешно проксированного запроса.
    /// Пример: <c>gateway.access.events</c>
    /// </summary>
    public string AccessEventsTopic { get; init; } = string.Empty;

    /// <summary>
    /// Имя топика Kafka для событий безопасности.
    /// Публикуется при неудачной JWT-валидации.
    /// Пример: <c>gateway.security.events</c>
    /// </summary>
    public string SecurityEventsTopic { get; init; } = string.Empty;

    /// <summary>
    /// Максимальная ёмкость локального in-memory буфера событий.
    /// При недоступности Kafka события накапливаются в буфере до этого предела.
    /// При переполнении новые события отбрасываются с логированием Warning.
    /// </summary>
    public int LocalBufferCapacity { get; init; } = 1000;
}
