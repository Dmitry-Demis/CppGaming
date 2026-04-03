namespace cppQuest.Server.Models;

public class ShopItem
{
    public string Id { get; set; } = "";          // "std_cpp20" | "content:chapter-2/...:1:20"
    public string Emoji { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string ItemType { get; set; } = "";    // "std_unlock" | "content_slot"

    // Стоимость (оба опциональны — хотя бы одно должно быть > 0)
    public int CostCoins { get; set; } = 0;
    public int CostKeys { get; set; } = 0;

    // Требования
    public int RequiredLevel { get; set; } = 1;
    public string? RequiredStd { get; set; }      // "c++20" — для content_slot

    public ICollection<UserPurchase> Purchases { get; set; } = [];
}
