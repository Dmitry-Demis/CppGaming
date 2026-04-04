namespace cppQuest.Server.Models;

/// <summary>
/// Запись о покупке предмета в магазине пользователем.
/// </summary>
public class UserPurchase
{
    /// <summary>PK</summary>
    public int Id { get; set; }

    /// <summary>FK на пользователя.</summary>
    public int UserId { get; set; }

    /// <summary>FK на ShopItem.Id.</summary>
    public string ItemId { get; set; } = "";

    /// <summary>Когда была совершена покупка.</summary>
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ShopItem Item { get; set; } = null!;
}
