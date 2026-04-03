using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using cppQuest.Server.Endpoints;
using cppQuest.Server.Models;
using cppQuest.Server.Services;
using cppQuest.Server.Repositories;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

// Database
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlite("Data Source=cppquest.db");
    opt.ConfigureWarnings(w => w.Ignore(
        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGamificationRepository, GamificationRepository>();
builder.Services.AddScoped<IStatsRepository, StatsRepository>();
builder.Services.AddScoped<IShopRepository, ShopRepository>();
builder.Services.AddScoped<ISlotRepository, SlotRepository>();
builder.Services.AddScoped<IQuestionProgressRepository, QuestionProgressRepository>();

// Services
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<GamificationService>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<QuestionProgressService>();

var app = builder.Build();

// Auto-migrate on startup
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler("/error");

// ---------------------------------------------------------
// ИСПРАВЛЕННАЯ ЛОГИКА ПОИСКА ПАПКИ CppCourse
// ---------------------------------------------------------
string baseDir = AppContext.BaseDirectory;       // Путь к папке публикации (где лежит .dll)
string currentDir = Directory.GetCurrentDirectory(); // Рабочая директория процесса

// Формируем кандидатов в порядке приоритета:
// 1. Рядом с DLL (Стандарт для опубликованного приложения: /var/www/myapp/CppCourse)
string candidate1 = Path.Combine(baseDir, "CppCourse");

// 2. Рядом с текущей директорией (Для запуска dotnet run из корня проекта)
string candidate2 = Path.Combine(currentDir, "CppCourse");

// 3. Для отладки в IDE: поднимаемся из bin/Debug/netX.X вверх к корню решения
//    bin/Debug/net8.0 -> ../../.. -> Корень решения
string candidate3 = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "CppCourse"));

// 4. Относительно ContentRootPath (на случай специфических настроек хостинга)
string candidate4 = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "CppCourse"));

string[] candidates = [candidate1, candidate2, candidate3, candidate4];

var cppCoursePath = candidates
    .Select(Path.GetFullPath) // Нормализация путей (убирает лишние .. и \)
    .FirstOrDefault(Directory.Exists)
    ?? throw new DirectoryNotFoundException(
        $"Папка CppCourse не найдена! Проверьте, что она скопирована при публикации.\n" +
        $"Проверенные пути:\n" +
        string.Join("\n", candidates.Select(c => $" - {c}"))
    );

app.Logger.LogInformation("Serving static files from: {Path}", cppCoursePath);
// ---------------------------------------------------------

// Map endpoints
app.MapAuthEndpoints();
app.MapProgressEndpoints();
app.MapQuizEndpoints();
app.MapCourseEndpoints(cppCoursePath);
app.MapFeedbackEndpoints();
app.MapAnalyticsEndpoints();
app.MapAchievementEndpoints();
app.MapLeaderboardEndpoints();

// Serve static files
app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(cppCoursePath),
    RequestPath = "",
    EnableDefaultFiles = true
});

await app.RunAsync();