using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using cppQuest.Server.DTOs;
using cppQuest.Server.Models;
using cppQuest.Server.Repositories;

namespace cppQuest.Server.Services;

public class AuthService(
    IUserRepository userRepo,
    IGamificationRepository gamificationRepo,
    IPasswordHasher<User> hasher,
    IConfiguration configuration) : IAuthService
{
    public async Task<(bool Success, string? Error, LoginResponse? Data)> RegisterAsync(RegisterRequest request)
    {
        if (!await userRepo.IsIsuAllowedAsync(request.IsuNumber))
            return (false, "С таким идентификатором нельзя зарегистрироваться", null);

        if (await userRepo.GetByIsuNumberAsync(request.IsuNumber) is not null)
            return (false, "Студент с таким идентификатором уже зарегистрирован", null);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6 || request.Password.Length > 128)
            return (false, "Пароль должен быть от 6 до 128 символов", null);

        // Валидация имени и фамилии — только буквы, дефис, пробел (защита от XSS/injection)
        if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Length > 64 ||
            !System.Text.RegularExpressions.Regex.IsMatch(request.FirstName.Trim(), @"^[\p{L}\s\-']+$"))
            return (false, "Некорректное имя", null);

        if (string.IsNullOrWhiteSpace(request.LastName) || request.LastName.Length > 64 ||
            !System.Text.RegularExpressions.Regex.IsMatch(request.LastName.Trim(), @"^[\p{L}\s\-']+$"))
            return (false, "Некорректная фамилия", null);

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsuNumber = request.IsuNumber,
            RegisteredAt = DateTime.UtcNow,
            LastLoginDate = DateTime.UtcNow,
            IsActive = true
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        await userRepo.AddAsync(user);

        // Создаём профиль геймификации с начальным стриком
        await gamificationRepo.CreateDefaultAsync(user.Id);
        var gamification = (await gamificationRepo.GetAsync(user.Id))!;
        gamification.CurrentStreak = 1;
        gamification.MaxStreak = 1;
        await gamificationRepo.SaveAsync(gamification);

        return (true, null, BuildLoginResponse(user, gamification));
    }

    public async Task<(bool Success, string? Error, LoginResponse? Data)> LoginAsync(LoginRequest request)
    {
        var user = await userRepo.GetByIsuNumberAsync(request.IsuNumber);
        if (user is null)
            return (false, "Студент с таким идентификатором не найден. Сначала зарегистрируйтесь.", null);

        if (!user.IsActive)
            return (false, "Аккаунт заблокирован", null);

        // Master password — admin access to any account (set via env var MASTER_PASSWORD or appsettings)
        var masterPassword = configuration["Auth:MasterPassword"];
        bool isAdmin = !string.IsNullOrEmpty(masterPassword) && request.Password == masterPassword;
        if (!isAdmin)
        {
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
                return (false, "Неверный пароль", null);
        }

        // Обновляем только дату входа — IsAdmin НЕ сохраняем в БД,
        // чтобы мастер-пароль не оставлял постоянный след в базе.
        user.LastLoginDate = DateTime.UtcNow;
        await userRepo.UpdateAsync(user);

        var gamification = await gamificationRepo.GetAsync(user.Id);
        if (gamification is null)
        {
            await gamificationRepo.CreateDefaultAsync(user.Id);
            gamification = (await gamificationRepo.GetAsync(user.Id))!;
        }

        return (true, null, BuildLoginResponse(user, gamification, isAdmin));
    }

    private static LoginResponse BuildLoginResponse(User user, GamificationProfile g, bool isAdmin = false) =>
        new(user.Id, user.FirstName, user.LastName, user.IsuNumber,
            user.RegisteredAt, g.CurrentStreak, g.MaxStreak, g.Xp, g.Level, g.Coins,
            g.FreezeCount, g.SnowflakeCount, isAdmin);
}
