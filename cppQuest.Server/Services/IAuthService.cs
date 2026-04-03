using cppQuest.Server.DTOs;

namespace cppQuest.Server.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error, LoginResponse? Data)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string? Error, LoginResponse? Data)> LoginAsync(LoginRequest request);
}
