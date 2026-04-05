using System.Text.RegularExpressions;
using cppQuest.Server.Repositories;
using cppQuest.Server.Services;

namespace cppQuest.Server.Endpoints;

/// <summary>
/// Вспомогательные методы, общие для нескольких endpoint-классов.
/// Вынесены сюда, чтобы не дублировать одну и ту же логику в каждом файле.
/// </summary>
internal static class EndpointHelpers
{
    /// <summary>
    /// C++98 доступен всем бесплатно — базовый набор стандартов без покупки.
    /// </summary>
    public static readonly HashSet<string> FreeStds = ["98"];

    // Только цифры и латинские буквы, максимум 12 символов
    private static readonly Regex IsuNumberRegex = new(@"^[A-Za-z0-9]{1,12}$", RegexOptions.Compiled);

    /// <summary>
    /// Читает ISU-номер из заголовка <c>X-Isu-Number</c>.
    /// Возвращает <c>null</c>, если заголовок отсутствует, пустой или не соответствует формату.
    /// Формат: только латинские буквы и цифры, максимум 12 символов.
    /// </summary>
    public static string? GetIsuNumber(HttpContext ctx)
    {
        var v = ctx.Request.Headers["X-Isu-Number"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(v))
        {
            return null;
        }
        
        var isMatch = IsuNumberRegex.IsMatch(v);
        
        return isMatch ? v : null;
    }

    /// <summary>
    /// Возвращает множество разблокированных стандартов C++ для текущего пользователя.
    /// C++98 включён всегда; остальные стандарты добавляются по покупкам вида <c>std_cpp{NN}</c>.
    /// </summary>
    /// <param name="profileService">Сервис профиля — нужен для получения userId по ISU.</param>
    /// <param name="shopRepo">Репозиторий магазина — источник покупок пользователя.</param>
    /// <param name="ctx">HTTP-контекст текущего запроса.</param>
    public static async Task<HashSet<string>> GetUnlockedStdsAsync(
        ProfileService profileService,
        IShopRepository shopRepo,
        HttpContext ctx)
    {
        var result = new HashSet<string>([.. FreeStds]);

        var isuNumber = GetIsuNumber(ctx);
        if (isuNumber is null) return result;

        var profile = await profileService.GetProfileAsync(isuNumber);
        if (profile is null) return result;

        var purchases = await shopRepo.GetUserPurchasesAsync(profile.Id);
        foreach (var p in purchases.Where(p => p.ItemId.StartsWith("std_cpp")))
            result.Add(p.ItemId["std_cpp".Length..]);   // "std_cpp17" → "17"

        return result;
    }

    /// <summary>
    /// Проверяет, что путь <paramref name="filePath"/> находится внутри <paramref name="root"/>.
    /// Защита от path traversal атак (/../).
    /// <param name="filePath">Полный путь к файлу (после <see cref="Path.GetFullPath"/>).</param>
    /// <param name="root">Корневая директория, за пределы которой выходить нельзя.</param>
    /// </summary>
    public static bool IsPathSafe(string filePath, string root) =>
        filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase);
}
