using cppQuest.Server.Services;

namespace cppQuest.Server.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");
        
        group.MapGet("profile/{isuNumber}", GetProfileAsync)
             .WithName("GetProfile")
             .RequireRateLimiting("api");
        
        // Эндпоинт для проверки существования пользователя (без IDOR-защиты)
        group.MapGet("validate-user/{isuNumber}", ValidateUserAsync)
             .WithName("ValidateUser")
             .RequireRateLimiting("api");
    }

    private static async Task<IResult> GetProfileAsync(
        string isuNumber,
        ProfileService profileService,
        HttpContext ctx,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("[PROFILE] GetProfile called for isuNumber: {IsuNumber}", isuNumber);
            
            // Проверяем, что запрашивают свой профиль
            var callerIsu = EndpointHelpers.GetIsuNumber(ctx);
            logger.LogInformation("[PROFILE] Caller isuNumber from header: {CallerIsu}", callerIsu ?? "NULL");
            
            if (callerIsu is null || callerIsu != isuNumber)
            {
                logger.LogWarning("[PROFILE] Unauthorized: callerIsu={CallerIsu}, requested={IsuNumber}", 
                    callerIsu ?? "NULL", isuNumber);
                return Results.Unauthorized();
            }

            var profile = await profileService.GetProfileAsync(isuNumber);
            
            if (profile is null)
            {
                logger.LogWarning("[PROFILE] Profile not found for isuNumber: {IsuNumber}", isuNumber);
                return Results.NotFound();
            }
            
            logger.LogInformation("[PROFILE] Profile found and returned for isuNumber: {IsuNumber}", isuNumber);
            return Results.Ok(profile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PROFILE] Exception in GetProfileAsync for isuNumber: {IsuNumber}", isuNumber);
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
    }

    // Эндпоинт для проверки существования пользователя (используется в auth-guard)
    private static async Task<IResult> ValidateUserAsync(
        string isuNumber,
        ProfileService profileService)
    {
        var profile = await profileService.GetProfileAsync(isuNumber);
        return profile is null 
            ? Results.NotFound(new { exists = false }) 
            : Results.Ok(new { exists = true });
    }
}
