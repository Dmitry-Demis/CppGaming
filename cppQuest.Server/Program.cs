using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using cppQuest.Server.Endpoints;
using cppQuest.Server.Models;
using cppQuest.Server.Services;
using cppQuest.Server.Repositories;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

// CSRF protection - настроено для HTTP (без Secure флага)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-Token";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false;   // JS должен читать куку
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;  // Разрешаем HTTP
});

// Rate limiting — защита от брутфорса на auth endpoints
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit         = 10;
        opt.Window              = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit          = 0;
    });
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit          = 60;
        opt.Window               = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit           = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

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
builder.Services.AddScoped<XpService>();
builder.Services.AddScoped<CurrencyService>();
builder.Services.AddScoped<StreakService>();
builder.Services.AddScoped<GamificationService>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<QuestionProgressService>();

var app = builder.Build();

// Exception handler must be first to catch all errors
app.UseExceptionHandler(o => o.Run(async ctx =>
{
    var exceptionFeature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var exception = exceptionFeature?.Error;
    
    app.Logger.LogError(exception, "[EXCEPTION] Unhandled exception: {Message}", exception?.Message);
    
    ctx.Response.StatusCode = 500;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsJsonAsync(new 
    { 
        message = "Internal server error",
        detail = app.Environment.IsDevelopment() ? exception?.Message : null,
        stackTrace = app.Environment.IsDevelopment() ? exception?.StackTrace : null
    });
}));

// Disable developer exception page to ensure consistent JSON error responses
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseRateLimiter();

// Security headers
app.Use(async (ctx, next) =>
{
    // Логируем каждый запрос
    app.Logger.LogInformation("[REQUEST] {Method} {Path} from {RemoteIp}", 
        ctx.Request.Method, 
        ctx.Request.Path, 
        ctx.Connection.RemoteIpAddress);
    
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    ctx.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    if (!ctx.Request.IsHttps)
    {
        // Не добавляем HSTS по HTTP — браузер всё равно его проигнорирует
    }
    else
    {
        ctx.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    await next(ctx);
    
    // Логируем ответ
    app.Logger.LogInformation("[RESPONSE] {Method} {Path} -> {StatusCode}", 
        ctx.Request.Method, 
        ctx.Request.Path, 
        ctx.Response.StatusCode);
});

// Выдаём CSRF-токен только для HTML-страниц (не для статики и API)
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? "";
    if (HttpMethods.IsGet(ctx.Request.Method)
        && !path.StartsWith("/api/")
        && (path.EndsWith(".html") || path == "/" || !path.Contains('.')))
    {
        var antiforgery = ctx.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
        antiforgery.SetCookieTokenAndHeader(ctx);
    }
    await next(ctx);
});

// Auto-migrate on startup
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

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
app.MapProfileEndpoints();
app.MapProgressEndpoints();
app.MapShopEndpoints();
app.MapGatedEndpoints();
app.MapQuizEndpoints();
app.MapCourseEndpoints(cppCoursePath);
app.MapFeedbackEndpoints();
app.MapAnalyticsEndpoints();
app.MapAchievementEndpoints();
app.MapLeaderboardEndpoints();

// Прогрев кэша структуры курса при старте
CourseEndpoints.WarmUp(cppCoursePath, app.Environment, app.Logger);

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