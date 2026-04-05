using cppQuest.Server.DTOs;
using cppQuest.Server.Models;
using cppQuest.Server.Repositories;
using Microsoft.Extensions.Logging;

namespace cppQuest.Server.Services;

public class ProfileService(
    IUserRepository userRepo, 
    IGamificationRepository gamificationRepo, 
    IShopRepository shopRepo,
    ILogger<ProfileService> logger)
{
    public async Task<ProfileResponse?> GetProfileAsync(string isuNumber)
    {
        try
        {
            logger.LogInformation("[PROFILE-SERVICE] GetProfileAsync called for isuNumber: {IsuNumber}", isuNumber);
            
            var user = await userRepo.GetByIsuNumberAsync(isuNumber);
            if (user is null)
            {
                logger.LogWarning("[PROFILE-SERVICE] User not found for isuNumber: {IsuNumber}", isuNumber);
                return null;
            }

            logger.LogInformation("[PROFILE-SERVICE] User found: Id={Id}, PublicId={PublicId}", user.Id, user.PublicId);

            var g = await gamificationRepo.GetAsync(user.Id);
            if (g is null)
            {
                logger.LogWarning("[PROFILE-SERVICE] Gamification profile not found for userId: {UserId}, creating default", user.Id);
                try
                {
                    await gamificationRepo.CreateDefaultAsync(user.Id);
                    g = await gamificationRepo.GetAsync(user.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[PROFILE-SERVICE] Failed to create gamification profile for userId: {UserId}", user.Id);
                    throw new InvalidOperationException($"Failed to create gamification profile for user {user.Id}", ex);
                }
                
                if (g is null)
                {
                    logger.LogError("[PROFILE-SERVICE] Gamification profile is still null after creation for userId: {UserId}", user.Id);
                    throw new InvalidOperationException($"Failed to create gamification profile for user {user.Id}");
                }
            }

            logger.LogInformation("[PROFILE-SERVICE] Gamification profile: Xp={Xp}, Level={Level}, Coins={Coins}, Keys={Keys}", 
                g.Xp, g.Level, g.Coins, g.Keys);

            logger.LogInformation("[PROFILE-SERVICE] Fetching user purchases for userId: {UserId}", user.Id);
            var purchases = new List<Models.UserPurchase>();
            try
            {
                purchases = await shopRepo.GetUserPurchasesAsync(user.Id);
                logger.LogInformation("[PROFILE-SERVICE] User has {Count} purchases", purchases.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PROFILE-SERVICE] Failed to fetch purchases for userId: {UserId}", user.Id);
                throw new InvalidOperationException($"Failed to fetch purchases for user {user.Id}", ex);
            }

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

            var response = new ProfileResponse(
                user.Id,
                user.PublicId,
                user.FirstName,
                user.LastName,
                user.IsuNumber,
                user.RegisteredAt,
                g.Xp,
                g.Level,
                g.Coins,
                g.Keys,
                g.CurrentStreak,
                g.MaxStreak,
                string.Join(',', unlockedStds),
                string.Join(',', unlockedSlots),
                user.IsAdmin
            );

            logger.LogInformation("[PROFILE-SERVICE] ProfileResponse created successfully for isuNumber: {IsuNumber}", isuNumber);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PROFILE-SERVICE] Exception in GetProfileAsync for isuNumber: {IsuNumber}", isuNumber);
            throw;
        }
    }
}
