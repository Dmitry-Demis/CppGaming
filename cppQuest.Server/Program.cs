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
// ПОИСК ПАПКИ CppCourse
// ---------------------------------------------------------
string baseDir = AppContext.BaseDirectory;
string currentDir = Directory.GetCurrentDirectory();

// В Development: берём путь из конфига (относительно ContentRootPath)
var configuredPath = builder.Configuration["CppCoursePath"];
string? cppCoursePath = null;

if (!string.IsNullOrEmpty(configuredPath))
{
    var resolved = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, configuredPath));
    if (Directory.Exists(resolved))
        cppCoursePath = resolved;
}

if (cppCoursePath == null)
{
    string[] candidates = [
        Path.Combine(baseDir, "CppCourse"),
        Path.Combine(currentDir, "CppCourse"),
        Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "CppCourse")),
        Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "CppCourse")),
    ];

    cppCoursePath = candidates
        .Select(Path.GetFullPath)
        .FirstOrDefault(Directory.Exists)
        ?? throw new DirectoryNotFoundException(
            $"Папка CppCourse не найдена!\nПроверенные пути:\n" +
            string.Join("\n", candidates.Select(c => $" - {c}"))
        );
}

app.Logger.LogInformation("Serving static files from: {Path}", cppCoursePath);
// ---------------------------------------------------------

// Map endpoints
app.MapAuthEndpoints();
app.MapProgressEndpoints();
app.MapShopEndpoints();
app.MapGatedEndpoints();
app.MapQuizEndpoints();
app.MapCourseEndpoints(cppCoursePath);
app.MapFeedbackEndpoints();
app.MapAnalyticsEndpoints();
app.MapAchievementEndpoints();
app.MapLeaderboardEndpoints();

// Serve static files
var fileProvider = new PhysicalFileProvider(cppCoursePath);
app.UseFileServer(new FileServerOptions
{
    FileProvider = fileProvider,
    RequestPath = "",
    EnableDefaultFiles = true,
    StaticFileOptions =
    {
        OnPrepareResponse = ctx =>
        {
            var path = ctx.File.Name;
            // JS и CSS — короткий кэш, чтобы браузер перепроверял
            ctx.Context.Response.Headers.CacheControl = path.EndsWith(".js") || path.EndsWith(".css") ? (Microsoft.Extensions.Primitives.StringValues)"no-cache" : (Microsoft.Extensions.Primitives.StringValues)"public, max-age=86400";
        }
    }
});

await app.RunAsync();