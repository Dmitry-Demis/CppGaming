using cppQuest.Server.DTOs;
using cppQuest.Server.Repositories;

namespace cppQuest.Server.Services;

public class ProfileService(IUserRepository userRepo, IGamificationRepository gamificationRepo, IShopRepository shopRepo)
{
    public async Task<ProfileResponse?> GetProfileAsync(string isuNumber)
    {
        var user = await userRepo.GetByIsuNumberAsync(isuNumber);
        if (user is null) return null;

        var g = await gamificationRepo.GetAsync(user.Id);
        var purchases = await shopRepo.GetUserPurchasesAsync(user.Id);

        // Собираем купленные стандарты из UserPurchases
        var unlockedStds = purchases
            .Where(p => p.ItemId.StartsWith("std_cpp"))
            .Select(p => "c++" + p.ItemId.Replace("std_cpp", ""))
            .ToList();

        // Собираем купленные слоты из UserPurchases
        var unlockedSlots = purchases
            .Where(p => p.ItemId.StartsWith("content:"))
            .Select(p => p.ItemId["content:".Length..])
            .ToList();

        return new ProfileResponse(
            user.Id,
            user.PublicId,
            user.FirstName,
            user.LastName,
            user.IsuNumber,
            user.RegisteredAt,
            g?.Xp ?? 0,
            g?.Level ?? 1,
            g?.Coins ?? 0,
            g?.Keys ?? 0,
            g?.CurrentStreak ?? 0,
            g?.MaxStreak ?? 0,
            string.Join(',', unlockedStds),
            string.Join(',', unlockedSlots),
            user.IsAdmin
        );
    }
}
