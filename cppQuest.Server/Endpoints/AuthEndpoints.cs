using cppQuest.Server.DTOs;
using cppQuest.Server.Services;

namespace cppQuest.Server.Endpoints;

/// <summary>
/// Расширение для регистрации конечных точек аутентификации.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Регистрирует конечные точки для регистрации и входа пользователей.
    /// </summary>
    /// <param name="app">Маршрутизатор конечных точек.</param>
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
                       .WithTags("Authentication");

        group.MapPost("register", HandleRegisterAsync)
             .WithName("RegisterUser")
             .RequireRateLimiting("auth");

        group.MapPost("login", HandleLoginAsync)
             .WithName("LoginUser")
             .RequireRateLimiting("auth");
    }

    /// <summary>
    /// Обрабатывает запрос на регистрацию нового пользователя.
    /// </summary>
    /// <param name="req">Данные для регистрации.</param>
    /// <param name="authService">Сервис аутентификации.</param>
    /// <returns>Результат операции: успех или ошибка.</returns>
    private static async Task<IResult> HandleRegisterAsync(RegisterRequest req, IAuthService authService)
    {
        var (success, error, data) = await authService.RegisterAsync(req);

        return success
            ? Results.Ok(new { message = "Регистрация прошла успешно!", data })
            : Results.BadRequest(new { message = error ?? "Произошла неизвестная ошибка." });
    }

    /// <summary>
    /// Обрабатывает запрос на вход существующего пользователя.
    /// </summary>
    /// <param name="req">Данные для входа.</param>
    /// <param name="authService">Сервис аутентификации.</param>
    /// <returns>Токен доступа или сообщение об ошибке.</returns>
    private static async Task<IResult> HandleLoginAsync(LoginRequest req, IAuthService authService)
    {
        var (success, error, data) = await authService.LoginAsync(req);

        return success
            ? Results.Ok(data)
            : Results.BadRequest(new { message = error ?? "Неверный логин или пароль." });
    }
}