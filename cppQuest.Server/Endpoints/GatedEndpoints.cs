using cppQuest.Server.DTOs;
using cppQuest.Server.Models;
using cppQuest.Server.Repositories;
using cppQuest.Server.Services;
using Microsoft.AspNetCore.Antiforgery;

namespace cppQuest.Server.Endpoints;

public static class GatedEndpoints
{
    public static void MapGatedEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        // GET /api/gated/all-slots — все gated-слоты во всех страницах + статус покупки
        group.MapGet("gated/all-slots", GetAllSlotsAsync);

        // GET /api/gated/slots?page=... — слоты конкретной страницы + статус покупки/разблокировки стандарта
        group.MapGet("gated/slots", GetPageSlotsAsync);

        // GET /api/gated/content?page=...&slot=...&std=... — HTML-содержимое купленного слота
        group.MapGet("gated/content", GetSlotContentAsync);

        // POST /api/gated/purchase — покупка отдельного gated-слота
        group.MapPost("gated/purchase", PurchaseSlotAsync).RequireRateLimiting("api");
    }

    // ── Handlers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Возвращает все gated-слоты из файловой системы с флагом <c>purchased</c> для текущего пользователя.
    /// </summary>
    private static async Task<IResult> GetAllSlotsAsync(
        ProfileService profileService,
        ISlotRepository slotRepo,
        HttpContext ctx,
        IWebHostEnvironment env)
    {
        var isuNumber = EndpointHelpers.GetIsuNumber(ctx);
        var profile   = isuNumber is null ? null : await profileService.GetProfileAsync(isuNumber);

        var gatedRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "gated"));
        if (!Directory.Exists(gatedRoot)) return Results.Ok((object[])[]);

        var purchased = await BuildPurchasedSetAsync(profile?.Id, slotRepo);
        var result    = await CollectAllSlotDefsAsync(gatedRoot, purchased);
        return Results.Ok(result);
    }

    /// <summary>
    /// Возвращает слоты одной страницы с флагами <c>purchased</c> и <c>stdUnlocked</c>.
    /// <c>stdUnlocked</c> = true, если стандарт бесплатный (98) или куплен в магазине.
    /// </summary>
    /// <param name="page">Относительный путь страницы, например <c>chapter-2/fundamental-types/signed-unsigned</c>.</param>
    private static async Task<IResult> GetPageSlotsAsync(
        [Microsoft.AspNetCore.Mvc.FromQuery] string page,
        ProfileService profileService,
        ISlotRepository slotRepo,
        IShopRepository shopRepo,
        HttpContext ctx,
        IWebHostEnvironment env)
    {
        var isuNumber = EndpointHelpers.GetIsuNumber(ctx);
        var profile   = isuNumber is null ? null : await profileService.GetProfileAsync(isuNumber);

        var gatedRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "gated"));
        var filePath  = Path.GetFullPath(Path.Combine(gatedRoot, page + ".html"));

        if (!EndpointHelpers.IsPathSafe(filePath, gatedRoot) || !File.Exists(filePath))
            return Results.Ok((object[])[]);

        var html          = await File.ReadAllTextAsync(filePath);
        var slotDefs      = GatedContentService.ExtractSlotDefs(html);
        var stdPurchases  = await GetStdPurchaseSetAsync(profile?.Id, shopRepo);
        var slotPurchases = await GetSlotPurchaseSetAsync(profile?.Id, page, slotRepo);

        var result = slotDefs.Select(s => new
        {
            slot        = s.Slot,
            std         = s.Std,
            costCoins   = s.CostCoins,
            costKeys    = s.CostKeys,
            purchased   = slotPurchases.Contains(s.Slot + ":" + s.Std),
            // Стандарт разблокирован, если он бесплатный или куплен в магазине
            stdUnlocked = EndpointHelpers.FreeStds.Contains(s.Std) || stdPurchases.Contains("std_cpp" + s.Std)
        });

        return Results.Ok(result);
    }

    /// <summary>
    /// Возвращает HTML-содержимое конкретного gated-слота.
    /// Требует, чтобы слот был предварительно куплен пользователем.
    /// </summary>
    /// <param name="page">Страница-владелец слота.</param>
    /// <param name="slot">Идентификатор слота внутри страницы.</param>
    /// <param name="std">Стандарт C++ (например, "17").</param>
    private static async Task<IResult> GetSlotContentAsync(
        [Microsoft.AspNetCore.Mvc.FromQuery] string page,
        [Microsoft.AspNetCore.Mvc.FromQuery] string slot,
        [Microsoft.AspNetCore.Mvc.FromQuery] string std,
        ProfileService profileService,
        ISlotRepository slotRepo,
        HttpContext ctx,
        IWebHostEnvironment env)
    {
        // ── Guards ───────────────────────────────────────────────────────────
        if (EndpointHelpers.GetIsuNumber(ctx) is not { } isuNumber) return Results.Unauthorized();

        var profile = await profileService.GetProfileAsync(isuNumber);
        if (EndpointGuards.RequireProfile(profile) is { } noProfile) return noProfile;

        if (await EndpointGuards.RequireSlotPurchasedAsync(slotRepo, profile!.Id, page, slot, std) is { } forbidden)
            return forbidden;

        // ── Бизнес-логика ────────────────────────────────────────────────────
        var gatedRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "gated"));
        var filePath  = Path.GetFullPath(Path.Combine(gatedRoot, page + ".html"));

        if (!EndpointHelpers.IsPathSafe(filePath, gatedRoot) || !File.Exists(filePath))
            return Results.NotFound();

        var html      = await File.ReadAllTextAsync(filePath);
        var extracted = GatedContentService.ExtractSlot(html, slot, std);
        return extracted is null ? Results.NotFound() : Results.Content(extracted, "text/html");
    }

    /// <summary>
    /// Покупает gated-слот за монеты/ключи.
    /// Цена читается из HTML-файла на сервере — клиент не может её подменить.
    /// </summary>
    private static async Task<IResult> PurchaseSlotAsync(
        GatedPurchaseRequest req,
        ProfileService profileService,
        GamificationService gamification,
        ISlotRepository slotRepo,
        IShopRepository shopRepo,
        IAntiforgery antiforgery,
        HttpContext ctx,
        IWebHostEnvironment env)
    {
        try { await antiforgery.ValidateRequestAsync(ctx); }
        catch { return Results.StatusCode(StatusCodes.Status403Forbidden); }

        // ── Guards ───────────────────────────────────────────────────────────
        if (EndpointHelpers.GetIsuNumber(ctx) is not { } isuNumber)
            return Results.BadRequest(new { message = "Нет заголовка X-Isu-Number" });

        var profile = await profileService.GetProfileAsync(isuNumber);
        if (EndpointGuards.RequireProfile(profile) is { } noProfile) return noProfile;

        if (await EndpointGuards.RequireStdUnlockedAsync(shopRepo, profile!.Id, req.Std) is { } stdError)
            return stdError;

        if (await EndpointGuards.RequireSlotNotPurchasedAsync(slotRepo, profile.Id, req.Page, req.Slot, req.Std) is { } alreadyBought)
            return alreadyBought;

        // ── Загрузка данных ──────────────────────────────────────────────────
        var slotDef = await ResolveSlotDefAsync(req, env);
        if (slotDef is null) return Results.NotFound(new { message = "Слот не найден" });

        if (EndpointGuards.RequireBalance(profile, slotDef.CostCoins, slotDef.CostKeys) is { } balanceError)
            return balanceError;

        // ── Бизнес-логика ────────────────────────────────────────────────────
        await DeductCurrencyAsync(gamification, profile.Id, slotDef.CostCoins, slotDef.CostKeys);

        await slotRepo.AddPurchaseAsync(new ParagraphSlotPurchase
        {
            UserId      = profile.Id,
            Page        = req.Page,
            Slot        = req.Slot,
            Std         = req.Std,
            CostCoins   = slotDef.CostCoins,
            CostKeys    = slotDef.CostKeys,
            PurchasedAt = DateTime.UtcNow
        });

        var updated = await profileService.GetProfileAsync(isuNumber);
        return Results.Ok(new { message = "Куплено", coins = updated!.Coins, keys = updated.Keys });
    }

    // ── Вспомогательные методы (данные) ─────────────────────────────────────

    /// <summary>
    /// Строит Set купленных слотов в формате <c>"page|slot|std"</c> для O(1) проверки.
    /// </summary>
    /// <param name="userId">Если <c>null</c> — возвращает пустой Set (анонимный пользователь).</param>
    private static async Task<HashSet<string>> BuildPurchasedSetAsync(int? userId, ISlotRepository slotRepo)
    {
        if (userId is null) return [];
        var purchases = await slotRepo.GetAllUserPurchasesAsync(userId.Value);
        return purchases.Select(p => $"{p.Page}|{p.Slot}|{p.Std}").ToHashSet();
    }

    /// <summary>Обходит все HTML-файлы в <paramref name="gatedRoot"/> и собирает метаданные слотов.</summary>
    private static async Task<List<object>> CollectAllSlotDefsAsync(
        string gatedRoot, HashSet<string> purchased)
    {
        List<object> result = [];
        foreach (var file in Directory.GetFiles(gatedRoot, "*.html", SearchOption.AllDirectories))
        {
            var page = Path.GetRelativePath(gatedRoot, file).Replace('\\', '/').Replace(".html", "");
            var html = await File.ReadAllTextAsync(file);
            foreach (var s in GatedContentService.ExtractSlotDefs(html))
            {
                result.Add(new
                {
                    page,
                    slot      = s.Slot,
                    std       = s.Std,
                    costCoins = s.CostCoins,
                    costKeys  = s.CostKeys,
                    purchased = purchased.Contains($"{page}|{s.Slot}|{s.Std}")
                });
            }
        }
        return result;
    }

    /// <summary>Возвращает Set ItemId купленных стандартов (например, <c>"std_cpp17"</c>).</summary>
    private static async Task<HashSet<string>> GetStdPurchaseSetAsync(int? userId, IShopRepository shopRepo)
    {
        if (userId is null) return [];
        return (await shopRepo.GetUserPurchasesAsync(userId.Value))
            .Select(p => p.ItemId).ToHashSet();
    }

    /// <summary>Возвращает Set купленных слотов страницы в формате <c>"slot:std"</c>.</summary>
    private static async Task<HashSet<string>> GetSlotPurchaseSetAsync(
        int? userId, string page, ISlotRepository slotRepo)
    {
        if (userId is null) return [];
        return (await slotRepo.GetUserPurchasesForPageAsync(userId.Value, page))
            .Select(p => p.Slot + ":" + p.Std).ToHashSet();
    }

    /// <summary>
    /// Читает определение слота из HTML-файла на сервере.
    /// Возвращает <c>null</c>, если файл не найден или слот не существует.
    /// </summary>
    private static async Task<SlotDef?> ResolveSlotDefAsync(GatedPurchaseRequest req, IWebHostEnvironment env)
    {
        var gatedRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "gated"));
        var filePath  = Path.GetFullPath(Path.Combine(gatedRoot, req.Page + ".html"));

        if (!EndpointHelpers.IsPathSafe(filePath, gatedRoot) || !File.Exists(filePath))
            return null;

        var html = await File.ReadAllTextAsync(filePath);
        return GatedContentService.ExtractSlotDefs(html)
            .FirstOrDefault(s => s.Slot == req.Slot && s.Std == req.Std);
    }

    /// <summary>Списывает монеты и ключи с gamification-профиля пользователя.</summary>
    private static async Task DeductCurrencyAsync(
        GamificationService gamification, int userId, int coins, int keys) =>
        await gamification.DeductCurrencyAsync(userId, coins, keys);
}
