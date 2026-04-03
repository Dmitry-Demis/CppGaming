using cppQuest.Server.DTOs;
using cppQuest.Server.Services;

namespace cppQuest.Server.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapPost("register", async (RegisterRequest req, IAuthService authService) =>
        {
            var (success, error, data) = await authService.RegisterAsync(req);
            return success
                ? Results.Ok(new { message = "Регистрация прошла успешно!", data })
                : Results.BadRequest(new { message = error });
        });

        group.MapPost("login", async (LoginRequest req, IAuthService authService) =>
        {
            var (success, error, data) = await authService.LoginAsync(req);
            return success
                ? Results.Ok(data)
                : Results.BadRequest(new { message = error });
        });
    }
}
