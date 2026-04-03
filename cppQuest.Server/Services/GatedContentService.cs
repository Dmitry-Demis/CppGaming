using cppQuest.Server.Models;
using cppQuest.Server.Repositories;
using System.Text.RegularExpressions;

namespace cppQuest.Server.Services;

public record SlotDef(string Slot, string Std, int CostCoins, int CostKeys);

public static class GatedContentService
{
    private static readonly Regex SectionRx = new(@"<section([^>]*)>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex AttrRx    = new(@"([\w-]+)=""([^""]*)""", RegexOptions.Compiled);

    public static async Task SyncAsync(string gatedRoot, IShopRepository shopRepo, ILogger logger)
    {
        if (!Directory.Exists(gatedRoot)) return;

        // Пересоздаём все content_slot записи при каждом старте
        await shopRepo.DeleteContentSlotsAsync();

        foreach (var file in Directory.GetFiles(gatedRoot, "*.html", SearchOption.AllDirectories))        {
            var page = Path.GetRelativePath(gatedRoot, file).Replace('\\', '/').Replace(".html", "");
            var html = await File.ReadAllTextAsync(file);

            foreach (Match sm in SectionRx.Matches(html))
            {
                var attrs = sm.Groups[1].Value;
                var map   = AttrRx.Matches(attrs).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);

                if (!map.TryGetValue("data-slot", out var slot) || !map.TryGetValue("data-std", out var std)) continue;

                var coins  = map.TryGetValue("coins", out var c) && int.TryParse(c, out var ci) ? ci : 0;
                var keys   = map.TryGetValue("keys",  out var k) && int.TryParse(k, out var ki) ? ki : 0;
                var itemId = $"content:{page}:{slot}:{std}";

                await shopRepo.AddItemAsync(new ShopItem
                {
                    Id            = itemId,
                    Emoji         = "🔒",
                    Name          = $"Слот {slot} · C++{std}",
                    ItemType      = "content_slot",
                    CostCoins     = coins,
                    CostKeys      = keys,
                    RequiredStd   = "c++" + std,
                    RequiredLevel = 1
                });
                logger.LogInformation("GatedContent: {ItemId} (coins={Coins}, keys={Keys})", itemId, coins, keys);
            }
        }
    }

    /// <summary>Возвращает все слоты из gated HTML файла с их атрибутами.</summary>
    public static List<SlotDef> ExtractSlotDefs(string html)
    {
        var result = new List<SlotDef>();
        foreach (Match sm in SectionRx.Matches(html))
        {
            var attrs = sm.Groups[1].Value;
            var map   = AttrRx.Matches(attrs).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
            if (!map.TryGetValue("data-slot", out var slot) || !map.TryGetValue("data-std", out var std)) continue;
            var coins = map.TryGetValue("coins", out var c) && int.TryParse(c, out var ci) ? ci : 0;
            var keys  = map.TryGetValue("keys",  out var k) && int.TryParse(k, out var ki) ? ki : 0;
            result.Add(new SlotDef(slot, std, coins, keys));
        }
        return result;
    }

    /// <summary>Извлекает HTML секции по slot+std из gated файла.</summary>
    public static string? ExtractSlot(string html, string slot, string std)
    {
        // Ищем <section ... slot="{slot}" ... std="{std}" ...>
        foreach (Match sm in SectionRx.Matches(html))
        {
            var attrs = sm.Groups[1].Value;
            var map   = AttrRx.Matches(attrs).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
            if (!map.TryGetValue("data-slot", out var s) || s != slot) continue;
            if (!map.TryGetValue("data-std",  out var d) || d != std)  continue;

            // Нашли нужную секцию — извлекаем до </section>
            var tagStart = sm.Index;
            int depth = 0, i = tagStart;
            while (i < html.Length)
            {
                if (html[i] == '<')
                {
                    if (i + 1 < html.Length && html[i + 1] == '/')
                    {
                        var end = html.IndexOf('>', i);
                        if (end > 0) { depth--; if (depth == 0) return html[tagStart..(end + 1)]; i = end + 1; continue; }
                    }
                    else if (i + 1 < html.Length && html[i + 1] != '!') depth++;
                }
                i++;
            }
        }
        return null;
    }
}
