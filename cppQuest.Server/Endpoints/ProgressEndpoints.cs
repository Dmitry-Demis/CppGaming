using cppQuest.Server.DTOs;
using cppQuest.Server.Models;
using cppQuest.Server.Repositories;
using cppQuest.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Endpoints;

public static class ProgressEndpoints
{
    public static void MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        // ── Stats ────────────────────────────────────────────────────────────

        group.MapGet("stats/avg-reading/{paragraphId}",
            async (string paragraphId, IStatsRepository statsRepo) =>
            Results.Ok(new { paragraphId, avgSeconds = (int)await statsRepo.GetAvgReadingSecondsAsync(paragraphId) }));

        group.MapPost("stats/avg-reading/bulk",
            async (List<string> paragraphIds, IStatsRepository statsRepo) =>
            Results.Ok(await statsRepo.GetAvgReadingSecondsBulkAsync(paragraphIds)));

        // ── Profile ──────────────────────────────────────────────────────────

        group.MapGet("profile/{isuNumber}", async (string isuNumber, ProfileService profileService) =>
        {
            var profile = await profileService.GetProfileAsync(isuNumber);
            return profile is not null ? Results.Ok(profile) : Results.NotFound();
        });

        group.MapGet("progress/{isuNumber}/{paragraphId}",
            async (string isuNumber, string paragraphId,
                   ProfileService profileService, StatsService statsService) =>
        {
            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.NotFound();
            return Results.Ok(await statsService.GetParagraphProgressAsync(profile.Id, paragraphId));
        });

        // GET /api/progress/{isuNumber}/unlocked-paragraphs
        // Возвращает Set<string> разблокированных paragraphId
        group.MapGet("progress/{isuNumber}/unlocked-paragraphs",
            async (string isuNumber, ProfileService profileService, IStatsRepository statsRepo, HttpContext ctx) =>
        {
            var isuHeader = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            if (string.IsNullOrEmpty(isuHeader)) return Results.Ok(new { unlocked = Array.Empty<string>(), isAdmin = false });

            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.Ok(new { unlocked = Array.Empty<string>(), isAdmin = false });

            // isAdmin — передаётся клиентом через заголовок X-Is-Admin (проверяем только для удобства UI)
            // Реальная проверка: сервер сам знает, т.к. isAdmin хранится в cpp_user на клиенте
            // Но для безопасности — isAdmin определяется только сервером через флаг в профиле
            // Пока используем заголовок X-Is-Admin (клиент честный, т.к. контент не секретный)
            bool isAdmin = ctx.Request.Headers["X-Is-Admin"].FirstOrDefault() == "1";

            var allTests = await statsRepo.GetAllTestStatsAsync(profile.Id);
            // bestScore по paragraphId
            var bestByParagraph = allTests
                .GroupBy(t => t.ParagraphId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Where(t => t.AttemptsCount > 0).Select(t => (int?)t.BestScore).DefaultIfEmpty(null).Min()
                );

            return Results.Ok(new { unlocked = bestByParagraph.Where(kv => kv.Value >= 70).Select(kv => kv.Key).ToArray(), isAdmin });
        });

        group.MapGet("progress/{isuNumber}/all",
            async (string isuNumber, ProfileService profileService, StatsService statsService) =>
        {
            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.NotFound();
            return Results.Ok(await statsService.GetAllProgressAsync(profile.Id));
        });

        group.MapPost("reading/complete",
            async (ReadingSessionRequest req, ProfileService profileService,
                   StatsService statsService, HttpContext ctx) =>
        {
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            if (string.IsNullOrEmpty(isuNumber)) return Results.BadRequest();
            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.NotFound();
            await statsService.CompleteReadingAsync(profile.Id, req);
            return Results.Ok();
        });

        // GET /api/review/due — параграфы с вопросами к повтору сегодня
        group.MapGet("review/due", async (
            ProfileService profileService, QuestionProgressService qpService, HttpContext ctx) =>
        {
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            if (string.IsNullOrEmpty(isuNumber)) return Results.Ok(Array.Empty<object>());
            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.Ok(Array.Empty<object>());

            var due = await qpService.GetDueParagraphsAsync(profile.Id);
            return Results.Ok(due);
        });

        group.MapGet("streak/{isuNumber}",
            async (string isuNumber, ProfileService profileService, GamificationService gamification) =>
        {
            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.NotFound();
            return Results.Ok(await gamification.CheckStreakOnLoginAsync(profile.Id));
        });

        // POST /api/scroll — добавить пиксели прокрутки (любая страница)
        group.MapPost("scroll", async (ScrollRequest req, ProfileService profileService,
            AppDbContext db, HttpContext ctx) =>
        {
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            if (string.IsNullOrEmpty(isuNumber) || req.Pixels <= 0) return Results.Ok();
            var user = await db.Users.FirstOrDefaultAsync(u => u.IsuNumber == isuNumber);
            if (user is null) return Results.Ok();
            await db.GamificationProfiles
                .Where(g => g.UserId == user.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    g => g.TotalScrollPixels,
                    g => g.TotalScrollPixels + req.Pixels));
            return Results.Ok();
        });

        group.MapPost("test/complete",
            async (TestCompleteRequest req, ProfileService profileService,
                   StatsService statsService, QuestionProgressService qpService,
                   HttpContext ctx) =>
        {
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            if (string.IsNullOrEmpty(isuNumber)) return Results.BadRequest();
            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.NotFound();

            // Обновляем прогресс по каждому вопросу
            var answers = new Dictionary<int, bool>();
            foreach (var id in req.CorrectQuestionIds ?? []) answers[id] = true;
            foreach (var id in req.WrongQuestionIds   ?? []) answers[id] = false;
            if (answers.Count > 0)
                await qpService.UpdateProgressAsync(profile.Id, req.TestId, req.ParagraphId, answers);

            var reward = await statsService.CompleteTestAsync(profile.Id, req);
            var updated = await profileService.GetProfileAsync(isuNumber);
            return Results.Ok(new { gamification = updated, reward });
        });

        // ── Shop ─────────────────────────────────────────────────────────────

        // GET /api/shop/items — каталог товаров
        group.MapGet("shop/items", async (IShopRepository shopRepo) =>
            Results.Ok(await shopRepo.GetAllItemsAsync()));

        // GET /api/gated/all-slots — все страницы с gated-слотами и статусом покупки
        group.MapGet("gated/all-slots", async (
            ProfileService profileService, ISlotRepository slotRepo,
            HttpContext ctx, IWebHostEnvironment env) =>
        {
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            var profile   = string.IsNullOrEmpty(isuNumber) ? null
                          : await profileService.GetProfileAsync(isuNumber);

            var gatedRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "gated"));
            if (!Directory.Exists(gatedRoot)) return Results.Ok(Array.Empty<object>());

            // Все купленные слоты пользователя
            var purchased = new HashSet<string>(); // "page|slot|std"
            if (profile is not null)
            {
                var allPurchases = await slotRepo.GetAllUserPurchasesAsync(profile.Id);
                foreach (var p in allPurchases)
                    purchased.Add($"{p.Page}|{p.Slot}|{p.Std}");
            }

            var result = new List<object>();
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
            return Results.Ok(result);
        });

        // POST /api/shop/purchase — покупка стандарта (std_unlock)
        group.MapPost("shop/purchase",
            async (ShopPurchaseRequest req, ProfileService profileService,
                   GamificationService gamification, IShopRepository shopRepo, HttpContext ctx) =>
        {
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            if (string.IsNullOrEmpty(isuNumber)) return Results.BadRequest(new { message = "Нет заголовка X-Isu-Number" });

            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.NotFound();

            var item = await shopRepo.GetItemAsync(req.ItemId);
            if (item is null) return Results.NotFound(new { message = "Товар не найден" });

            if (await shopRepo.HasPurchasedAsync(profile.Id, req.ItemId))
                return Results.BadRequest(new { message = "Уже куплено" });

            if (profile.Level < item.RequiredLevel)
                return Results.BadRequest(new { message = "Недостаточный уровень" });
            if (profile.Coins < item.CostCoins)
                return Results.BadRequest(new { message = "Недостаточно монет" });
            if (profile.Keys < item.CostKeys)
                return Results.BadRequest(new { message = "Недостаточно ключей" });

            var gProfile = await gamification.GetAsync(profile.Id);
            if (gProfile is null) return Results.NotFound();

            if (item.CostCoins > 0) gProfile.Coins -= item.CostCoins;
            if (item.CostKeys  > 0) gProfile.Keys  -= item.CostKeys;
            await gamification.SaveAsync(gProfile);

            await shopRepo.AddPurchaseAsync(new UserPurchase
            {
                UserId = profile.Id, ItemId = req.ItemId, PurchasedAt = DateTime.UtcNow
            });

            var updated = await profileService.GetProfileAsync(isuNumber);
            return Results.Ok(new { message = "Куплено", coins = updated!.Coins, keys = updated.Keys });
        });

        // ── Gated content ────────────────────────────────────────────────────

        // GET /api/gated/slots?page=chapter-2/fundamental-types/signed-unsigned
        group.MapGet("gated/slots", async (
            [Microsoft.AspNetCore.Mvc.FromQuery] string page,
            ProfileService profileService, ISlotRepository slotRepo, IShopRepository shopRepo,
            HttpContext ctx, IWebHostEnvironment env) =>
        {
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            var profile   = string.IsNullOrEmpty(isuNumber) ? null
                          : await profileService.GetProfileAsync(isuNumber);

            // Читаем слоты прямо из gated HTML
            var filePath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "gated", page + ".html"));
            var gatedRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "gated"));
            if (!filePath.StartsWith(gatedRoot) || !File.Exists(filePath))
                return Results.Ok(Array.Empty<object>());

            var html = await File.ReadAllTextAsync(filePath);
            var slotDefs = GatedContentService.ExtractSlotDefs(html);

            // Купленные стандарты пользователя (из магазина)
            var stdPurchases = profile is not null
                ? (await shopRepo.GetUserPurchasesAsync(profile.Id))
                    .Select(p => p.ItemId).ToHashSet()
                : new HashSet<string>();

            // Базовые стандарты (98) не требуют покупки
            var freeStds = new HashSet<string> { "98" };

            // Купленные слоты параграфа (из новой таблицы) — ключ slot:std
            var slotPurchases = profile is not null
                ? (await slotRepo.GetUserPurchasesForPageAsync(profile.Id, page))
                    .Select(p => p.Slot + ":" + p.Std).ToHashSet()
                : new HashSet<string>();

            var result = slotDefs.Select(s => new
            {
                slot        = s.Slot,
                std         = s.Std,
                costCoins   = s.CostCoins,
                costKeys    = s.CostKeys,
                purchased   = slotPurchases.Contains(s.Slot + ":" + s.Std),
                stdUnlocked = freeStds.Contains(s.Std) || stdPurchases.Contains("std_cpp" + s.Std)
            });

            return Results.Ok(result);
        });

        // GET /api/gated/content?page=...&slot=1&std=20
        group.MapGet("gated/content", async (
            [Microsoft.AspNetCore.Mvc.FromQuery] string page,
            [Microsoft.AspNetCore.Mvc.FromQuery] string slot,
            [Microsoft.AspNetCore.Mvc.FromQuery] string std,
            ProfileService profileService, ISlotRepository slotRepo,
            HttpContext ctx, IWebHostEnvironment env) =>
        {
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            if (string.IsNullOrEmpty(isuNumber)) return Results.Unauthorized();
            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.Unauthorized();

            if (!await slotRepo.HasPurchasedAsync(profile.Id, page, slot, std))
                return Results.Forbid();

            var filePath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "gated", page + ".html"));
            var gatedRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "gated"));
            if (!filePath.StartsWith(gatedRoot) || !File.Exists(filePath)) return Results.NotFound();

            var html = await File.ReadAllTextAsync(filePath);
            var extracted = GatedContentService.ExtractSlot(html, slot, std);
            return extracted is null ? Results.NotFound() : Results.Content(extracted, "text/html");
        });

        // POST /api/gated/purchase
        group.MapPost("gated/purchase",
            async (GatedPurchaseRequest req, ProfileService profileService,
                   GamificationService gamification, ISlotRepository slotRepo,
                   IShopRepository shopRepo, HttpContext ctx, IWebHostEnvironment env) =>
        {
            var isuNumber = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
            if (string.IsNullOrEmpty(isuNumber)) return Results.BadRequest(new { message = "Нет заголовка X-Isu-Number" });

            var profile = await profileService.GetProfileAsync(isuNumber);
            if (profile is null) return Results.NotFound();

            // Стандарт должен быть куплен в магазине (C++98 — бесплатный)
            var freeStds2 = new HashSet<string> { "98" };
            if (!freeStds2.Contains(req.Std) && !await shopRepo.HasPurchasedAsync(profile.Id, "std_cpp" + req.Std))
                return Results.BadRequest(new { message = "Стандарт не разблокирован" });

            if (await slotRepo.HasPurchasedAsync(profile.Id, req.Page, req.Slot, req.Std))
                return Results.BadRequest(new { message = "Уже куплено" });

            // Читаем цену из gated HTML
            var filePath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "gated", req.Page + ".html"));
            var gatedRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "gated"));
            if (!filePath.StartsWith(gatedRoot) || !File.Exists(filePath))
                return Results.NotFound(new { message = "Слот не найден" });

            var html = await File.ReadAllTextAsync(filePath);
            var slotDef = GatedContentService.ExtractSlotDefs(html)
                .FirstOrDefault(s => s.Slot == req.Slot && s.Std == req.Std);
            if (slotDef is null) return Results.NotFound(new { message = "Слот не найден" });

            if (profile.Coins < slotDef.CostCoins)
                return Results.BadRequest(new { message = "Недостаточно монет" });
            if (profile.Keys < slotDef.CostKeys)
                return Results.BadRequest(new { message = "Недостаточно ключей" });

            var gProfile = await gamification.GetAsync(profile.Id);
            if (gProfile is null) return Results.NotFound();
            if (slotDef.CostCoins > 0) gProfile.Coins -= slotDef.CostCoins;
            if (slotDef.CostKeys  > 0) gProfile.Keys  -= slotDef.CostKeys;
            await gamification.SaveAsync(gProfile);

            await slotRepo.AddPurchaseAsync(new ParagraphSlotPurchase
            {
                UserId    = profile.Id,
                Page      = req.Page,
                Slot      = req.Slot,
                Std       = req.Std,
                CostCoins = slotDef.CostCoins,
                CostKeys  = slotDef.CostKeys,
                PurchasedAt = DateTime.UtcNow
            });

            var updated = await profileService.GetProfileAsync(isuNumber);
            return Results.Ok(new { message = "Куплено", coins = updated!.Coins, keys = updated.Keys });
        });
    }

}

