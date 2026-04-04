namespace cppQuest.Server.Models;

/// <summary>
/// Элемент магазина (ShopItem). Может быть разблокировкой стандарта или платным слотом.
/// </summary>
public class ShopItem
{
    /// <summary>Идентификатор предмета, например "std_cpp20" или контентный ключ.</summary>
    public string Id { get; set; } = "";          // "std_cpp20" | "content:chapter-2/...:1:20"

    /// <summary>Эмодзи-иконка для UI.</summary>
    public string Emoji { get; set; } = "";

    /// <summary>Читабельное название предмета.</summary>
    public string Name { get; set; } = "";

    /// <summary>Текстовое описание предмета.</summary>
    public string? Description { get; set; }

    /// <summary>Тип предмета: "std_unlock" или "content_slot" и т.д.</summary>
    public string ItemType { get; set; } = "";    // "std_unlock" | "content_slot"

    // Стоимость (оба опциональны — хотя бы одно должно быть > 0)
    public int CostCoins { get; set; } = 0;
    public int CostKeys { get; set; } = 0;

    // Требования
    public int RequiredLevel { get; set; } = 1;

    /// <summary>Опционально — требуемый стандарт C++ для контент-слота (например "c++20").</summary>
    public string? RequiredStd { get; set; }

    /// <summary>Навигация — все покупки этого предмета.</summary>
    public ICollection<UserPurchase> Purchases { get; set; } = [];
}
