namespace cppQuest.Server.Models;

public class UserPurchase
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ItemId { get; set; } = "";
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ShopItem Item { get; set; } = null!;
}
