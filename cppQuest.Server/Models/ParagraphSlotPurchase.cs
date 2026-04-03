namespace cppQuest.Server.Models;

/// <summary>
/// Хранит факт покупки платного слота внутри параграфа.
/// Не связана с магазином (ShopItems/UserPurchases) — только параграфы.
/// </summary>
public class ParagraphSlotPurchase
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Страница параграфа: "chapter-2/fundamental-types/signed-unsigned"</summary>
    public string Page { get; set; } = "";

    /// <summary>Номер слота на странице (data-slot)</summary>
    public string Slot { get; set; } = "";

    /// <summary>Стандарт C++ (data-std): "20", "17" и т.д.</summary>
    public string Std { get; set; } = "";

    /// <summary>Стоимость на момент покупки</summary>
    public int CostCoins { get; set; }
    public int CostKeys { get; set; }

    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
