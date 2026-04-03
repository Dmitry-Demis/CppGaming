using cppQuest.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Repositories;

public class ShopRepository(AppDbContext db) : IShopRepository
{
    public Task<List<ShopItem>> GetAllItemsAsync() =>
        db.ShopItems.ToListAsync();

    public Task<ShopItem?> GetItemAsync(string itemId) =>
        db.ShopItems.FindAsync(itemId).AsTask();

    public Task<List<UserPurchase>> GetUserPurchasesAsync(int userId) =>
        db.UserPurchases.Where(p => p.UserId == userId).ToListAsync();

    public Task<bool> HasPurchasedAsync(int userId, string itemId) =>
        db.UserPurchases.AnyAsync(p => p.UserId == userId && p.ItemId == itemId);

    public async Task AddPurchaseAsync(UserPurchase purchase)
    {
        db.UserPurchases.Add(purchase);
        await db.SaveChangesAsync();
    }

    public async Task AddItemAsync(ShopItem item)
    {
        db.ShopItems.Add(item);
        await db.SaveChangesAsync();
    }

    public async Task UpdateItemAsync(ShopItem item)
    {
        db.ShopItems.Update(item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteContentSlotsAsync()
    {
        var slots = await db.ShopItems.Where(i => i.ItemType == "content_slot").ToListAsync();
        db.ShopItems.RemoveRange(slots);
        await db.SaveChangesAsync();
    }

    public Task<List<ShopItem>> GetItemsByPageAsync(string page) =>
        db.ShopItems
            .Where(i => i.Id.StartsWith($"content:{page}:"))
            .ToListAsync();
}
