using cppQuest.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Repositories;

public class SlotRepository(AppDbContext db) : ISlotRepository
{
    public Task<bool> HasPurchasedAsync(int userId, string page, string slot, string std) =>
        db.ParagraphSlotPurchases.AnyAsync(p =>
            p.UserId == userId && p.Page == page && p.Slot == slot && p.Std == std);

    public async Task AddPurchaseAsync(ParagraphSlotPurchase purchase)
    {
        db.ParagraphSlotPurchases.Add(purchase);
        await db.SaveChangesAsync();
    }

    public Task<List<ParagraphSlotPurchase>> GetUserPurchasesForPageAsync(int userId, string page) =>
        db.ParagraphSlotPurchases
            .Where(p => p.UserId == userId && p.Page == page)
            .ToListAsync();

    public Task<List<ParagraphSlotPurchase>> GetAllUserPurchasesAsync(int userId) =>
        db.ParagraphSlotPurchases
            .Where(p => p.UserId == userId)
            .ToListAsync();
}
