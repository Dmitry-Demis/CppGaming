using CppQuest.Gateway.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Включаем отслеживание изменений appsettings.json для Hot Reload.
// При изменении файла конфигурация перезагружается без перезапуска процесса.
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true, reloadOnChange: true);

// Привязка корневой конфигурации Gateway к IOptions<GatewayConfiguration>
builder.Services.Configure<GatewayConfiguration>(builder.Configuration);

// Привязка отдельных секций для удобного внедрения через IOptionsMonitor<T>.
// IOptionsMonitor автоматически получает обновлённые значения при Hot Reload —
// без перезапуска сервиса достаточно изменить appsettings.json.
builder.Services.Configure<RateLimitingConfig>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<CorsConfig>(builder.Configuration.GetSection("Cors"));
builder.Services.Configure<KafkaConfig>(builder.Configuration.GetSection("Kafka"));

// Регистрация YARP Reverse Proxy.
// LoadFromConfig читает секцию ReverseProxy из IConfiguration и подписывается
// на её изменения через IOptionsMonitor — новые маршруты применяются в течение 5 секунд.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Конвейер middleware будет расширен в задачах 3–12:
// CORS → Rate Limiter → JWT Validation → Header Transform → YARP (Polly + Metrics)
app.MapReverseProxy();

app.Run();

// Открываем класс Program для WebApplicationFactory в интеграционных тестах
public partial class Program { }
