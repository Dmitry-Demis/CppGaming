using cppQuest.Server.Models;

namespace cppQuest.Server.Repositories;

public interface IShopRepository
{
    Task<List<ShopItem>> GetAllItemsAsync();
    Task<ShopItem?> GetItemAsync(string itemId);
    Task<List<UserPurchase>> GetUserPurchasesAsync(int userId);
    Task<bool> HasPurchasedAsync(int userId, string itemId);
    Task AddPurchaseAsync(UserPurchase purchase);
    Task AddItemAsync(ShopItem item);
    Task UpdateItemAsync(ShopItem item);
    Task DeleteContentSlotsAsync();
    Task<List<ShopItem>> GetItemsByPageAsync(string page);
}
