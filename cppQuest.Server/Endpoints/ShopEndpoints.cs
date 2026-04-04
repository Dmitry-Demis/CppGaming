using cppQuest.Server.DTOs;
using cppQuest.Server.Models;
using cppQuest.Server.Repositories;
using cppQuest.Server.Services;

namespace cppQuest.Server.Endpoints;

public static class ShopEndpoints
{
    public static void MapShopEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        // GET /api/shop/items — каталог товаров магазина
        group.MapGet("shop/items", async (IShopRepository shopRepo) =>
            Results.Ok(await shopRepo.GetAllItemsAsync()));

        // POST /api/shop/purchase — покупка товара (стандарт C++, скин и т.д.)
        group.MapPost("shop/purchase", PurchaseItemAsync);
    }

    /// <summary>
    /// Покупает товар из магазина.
    /// Валидация и guard-проверки делегированы в <see cref="EndpointGuards"/>.
    /// </summary>
    /// <param name="req">Тело запроса: идентификатор товара.</param>
    private static async Task<IResult> PurchaseItemAsync(
        ShopPurchaseRequest req,
        ProfileService profileService,
        GamificationService gamification,
        IShopRepository shopRepo,
        HttpContext ctx)
    {
        // ── Guards ───────────────────────────────────────────────────────────
        if (EndpointHelpers.GetIsuNumber(ctx) is not { } isuNumber)
            return Results.BadRequest(new { message = "Нет заголовка X-Isu-Number" });

        var profile = await profileService.GetProfileAsync(isuNumber);
        if (EndpointGuards.RequireProfile(profile) is { } noProfile) return noProfile;

        var item = await shopRepo.GetItemAsync(req.ItemId);
        if (item is null) return Results.NotFound(new { message = "Товар не найден" });

        if (await EndpointGuards.RequireNotPurchasedAsync(shopRepo, profile!.Id, req.ItemId) is { } alreadyBought)
            return alreadyBought;

        if (EndpointGuards.RequireLevel(profile, item.RequiredLevel) is { } levelError) return levelError;
        if (EndpointGuards.RequireBalance(profile, item.CostCoins, item.CostKeys) is { } balanceError) return balanceError;

        // ── Бизнес-логика ────────────────────────────────────────────────────
        await DeductAndSaveAsync(gamification, profile.Id, item.CostCoins, item.CostKeys);

        await shopRepo.AddPurchaseAsync(new UserPurchase
        {
            UserId = profile.Id, ItemId = req.ItemId, PurchasedAt = DateTime.UtcNow
        });

        var updated = await profileService.GetProfileAsync(isuNumber);
        return Results.Ok(new { message = "Куплено", coins = updated!.Coins, keys = updated.Keys });
    }

    /// <summary>Списывает монеты и ключи с gamification-профиля.</summary>
    private static async Task DeductAndSaveAsync(
        GamificationService gamification, int userId, int coins, int keys)
    {
        var gProfile = await gamification.GetAsync(userId)
            ?? throw new InvalidOperationException($"GamificationProfile not found for userId={userId}");
        if (coins > 0) gProfile.Coins -= coins;
        if (keys  > 0) gProfile.Keys  -= keys;
        await gamification.SaveAsync(gProfile);
    }
}
