using cppQuest.Server.DTOs;
using cppQuest.Server.Repositories;
using cppQuest.Server.Services;

namespace cppQuest.Server.Endpoints;

/// <summary>
/// Guard-проверки для endpoint-обработчиков.
/// Каждый метод отвечает за одну конкретную проверку и возвращает
/// <c>IResult</c> с ошибкой или <c>null</c>, если проверка пройдена.
///
/// Принцип: endpoint-handler не знает, как проверять — он только вызывает guard
/// и решает, продолжать ли выполнение.
/// </summary>
internal static class EndpointGuards
{
    /// <summary>
    /// Проверяет наличие заголовка <c>X-Isu-Number</c>.
    /// </summary>
    public static IResult? RequireIsuHeader(HttpContext ctx) =>
        EndpointHelpers.GetIsuNumber(ctx) is null
            ? Results.BadRequest(new { message = "Нет заголовка X-Isu-Number" })
            : null;

    /// <summary>
    /// Проверяет, что профиль пользователя существует в БД.
    /// </summary>
    /// <param name="profile">Результат запроса профиля — может быть <c>null</c>.</param>
    public static IResult? RequireProfile(ProfileResponse? profile) =>
        profile is null ? Results.NotFound() : null;

    /// <summary>
    /// Проверяет, что у пользователя достаточно монет и ключей для покупки.
    /// </summary>
    public static IResult? RequireBalance(ProfileResponse profile, int costCoins, int costKeys)
    {
        if (profile.Coins < costCoins)
            return Results.BadRequest(new { message = "Недостаточно монет" });
        if (profile.Keys < costKeys)
            return Results.BadRequest(new { message = "Недостаточно ключей" });
        return null;
    }

    /// <summary>
    /// Проверяет, что пользователь ещё не купил товар в магазине (идемпотентность).
    /// </summary>
    public static async Task<IResult?> RequireNotPurchasedAsync(
        IShopRepository shopRepo, int userId, string itemId) =>
        await shopRepo.HasPurchasedAsync(userId, itemId)
            ? Results.BadRequest(new { message = "Уже куплено" })
            : null;

    /// <summary>
    /// Проверяет, что уровень пользователя достаточен для покупки товара.
    /// </summary>
    public static IResult? RequireLevel(ProfileResponse profile, int requiredLevel) =>
        profile.Level < requiredLevel
            ? Results.BadRequest(new { message = "Недостаточный уровень" })
            : null;

    /// <summary>
    /// Проверяет, что стандарт C++ разблокирован (бесплатный или куплен в магазине).
    /// </summary>
    public static async Task<IResult?> RequireStdUnlockedAsync(
        IShopRepository shopRepo, int userId, string std) =>
        !EndpointHelpers.FreeStds.Contains(std) && !await shopRepo.HasPurchasedAsync(userId, "std_cpp" + std)
            ? Results.BadRequest(new { message = "Стандарт не разблокирован" })
            : null;

    /// <summary>
    /// Проверяет, что gated-слот ещё не куплен пользователем.
    /// </summary>
    public static async Task<IResult?> RequireSlotNotPurchasedAsync(
        ISlotRepository slotRepo, int userId, string page, string slot, string std) =>
        await slotRepo.HasPurchasedAsync(userId, page, slot, std)
            ? Results.BadRequest(new { message = "Уже куплено" })
            : null;

    /// <summary>
    /// Проверяет, что пользователь купил gated-слот (для доступа к контенту).
    /// </summary>
    public static async Task<IResult?> RequireSlotPurchasedAsync(
        ISlotRepository slotRepo, int userId, string page, string slot, string std) =>
        !await slotRepo.HasPurchasedAsync(userId, page, slot, std)
            ? Results.Forbid()
            : null;
}
