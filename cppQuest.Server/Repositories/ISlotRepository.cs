using cppQuest.Server.Models;

namespace cppQuest.Server.Repositories;

public interface ISlotRepository
{
    Task<bool> HasPurchasedAsync(int userId, string page, string slot, string std);
    Task AddPurchaseAsync(ParagraphSlotPurchase purchase);
    Task<List<ParagraphSlotPurchase>> GetUserPurchasesForPageAsync(int userId, string page);
    Task<List<ParagraphSlotPurchase>> GetAllUserPurchasesAsync(int userId);
}
