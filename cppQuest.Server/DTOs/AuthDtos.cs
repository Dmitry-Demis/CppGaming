namespace cppQuest.Server.DTOs;

public record RegisterRequest(string FirstName, string LastName, string IsuNumber, string Password);

public record LoginRequest(string IsuNumber, string Password);

public record LoginResponse(
    int Id,
    string FirstName,
    string LastName,
    string IsuNumber,
    DateTime RegisteredAt,
    int CurrentStreak,
    int MaxStreak,
    int Xp,
    int Level,
    int Coins,
    int FreezeCount = 0,
    int SnowflakeCount = 0,
    bool IsAdmin = false
);
