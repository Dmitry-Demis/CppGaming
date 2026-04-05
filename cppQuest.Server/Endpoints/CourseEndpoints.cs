using System.Text.Json;

namespace cppQuest.Server.Endpoints;

public static class CourseEndpoints
{
    // Кэш структуры курса — строится один раз при первом запросе
    private static object? _cache;
    private static readonly Lock _lock = new();

    /// <param name="cppCoursePath">Абсолютный путь к директории фронтенда курса (содержит course-meta.json, theory/).</param>
    public static void MapCourseEndpoints(this IEndpointRouteBuilder app, string cppCoursePath)
    {
        var api = app.MapGroup("/api");
        api.MapGet("examples/{**path}",  (string path, IWebHostEnvironment env) => ServeStaticFile(path, env, "examples", null));
        api.MapGet("tests/{**path}",     (string path, IWebHostEnvironment env) => ServeStaticFile(path, env, "tests", "application/json"));
        api.MapGet("course-structure",   (IWebHostEnvironment env)               => GetCourseStructure(cppCoursePath, env));
    }

    /// <summary>Прогревает кэш структуры курса при старте сервера.</summary>
    public static void WarmUp(string cppCoursePath, IWebHostEnvironment env, ILogger logger)
    {
        logger.LogInformation("[CourseEndpoints] Warming up course structure cache...");
        BuildCache(cppCoursePath, env);
        logger.LogInformation("[CourseEndpoints] Course structure cache ready.");
    }

    /// <summary>
    /// Отдаёт файл из защищённой поддиректории <paramref name="subDir"/> внутри ContentRoot.
    /// Защита от path traversal: проверяет, что итоговый путь остаётся внутри корневой директории.
    /// </summary>
    /// <param name="path">Относительный путь к файлу, пришедший из URL.</param>
    /// <param name="subDir">Поддиректория внутри ContentRoot (например, "examples" или "tests").</param>
    /// <param name="contentType">
    ///     Content-Type ответа. Если <c>null</c> — возвращает объект <c>{ path, code }</c> вместо сырого текста.
    /// </param>
    private static IResult ServeStaticFile(string path, IWebHostEnvironment env, string subDir, string? contentType)
    {
        if (path.Contains("..")) return Results.BadRequest();

        var root     = Path.GetFullPath(Path.Combine(env.ContentRootPath, subDir));
        var filePath = Path.GetFullPath(Path.Combine(root, path));

        if (!EndpointHelpers.IsPathSafe(filePath, root)) return Results.BadRequest();
        if (!File.Exists(filePath))                       return Results.NotFound();

        var text = File.ReadAllText(filePath);
        return contentType is not null
            ? Results.Content(text, contentType)
            : Results.Ok(new { path, code = text });
    }

    /// <summary>
    /// Строит полную структуру курса из <c>course-meta.json</c>:
    /// главы → параграфы → тесты + примеры кода.
    /// Результат кэшируется в памяти на всё время жизни процесса.
    /// </summary>
    /// <param name="cppCoursePath">Путь к директории фронтенда курса.</param>
    private static IResult GetCourseStructure(string cppCoursePath, IWebHostEnvironment env)
    {
        var cached = BuildCache(cppCoursePath, env);
        return cached is null
            ? Results.NotFound(new { message = "course-meta.json not found" })
            : Results.Ok(cached);
    }

    private static object? BuildCache(string cppCoursePath, IWebHostEnvironment env)
    {
        if (_cache is not null) return _cache;
        lock (_lock)
        {
            if (_cache is not null) return _cache;

            var metaPath = Path.Combine(cppCoursePath, "course-meta.json");
            if (!File.Exists(metaPath)) return null;

            using var meta = JsonDocument.Parse(File.ReadAllText(metaPath));
            var testsRoot  = Path.Combine(env.ContentRootPath, "tests");
            var theoryRoot = Path.Combine(cppCoursePath, "theory");

            var chapters = meta.RootElement.GetProperty("chapters").EnumerateArray()
                .Select(ch => BuildChapter(ch, theoryRoot, testsRoot, env.ContentRootPath))
                .ToList();

            _cache = new { chapters };
            return _cache;
        }
    }

    /// <summary>
    /// Строит объект главы: читает метаданные из JSON и собирает список параграфов.
    /// </summary>
    private static object BuildChapter(JsonElement ch, string theoryRoot, string testsRoot, string contentRoot)
    {
        var chapterId = ch.GetProperty("id").GetString()!;
        var groupId   = ch.GetProperty("groupId").GetString()!;
        var title     = ch.GetProperty("title").GetString();
        var desc      = ch.TryGetProperty("description", out var d)    ? d.GetString()   : null;
        var level     = ch.TryGetProperty("level",       out var lv)   ? lv.GetString()  : null;
        var chNum     = ch.TryGetProperty("number",      out var num)  ? num.GetInt32()  : 0;

        var theoryDir     = Path.Combine(theoryRoot,   chapterId, groupId);
        var chTestsDir    = Path.Combine(testsRoot,    chapterId, groupId);
        var chExamplesDir = Path.Combine(contentRoot, "examples", chapterId, groupId);

        var paragraphs = GetParaIds(ch, theoryDir)
            .Select(id => BuildParagraph(id, theoryDir, chTestsDir, chExamplesDir))
            .Cast<object>()
            .ToList();

        return new { id = chapterId, number = chNum, groupId, title, description = desc, level, paragraphs };
    }

    /// <summary>
    /// Возвращает список ID параграфов главы.
    /// Приоритет: явный список в JSON → файлы *.html в директории теории.
    /// </summary>
    private static IEnumerable<string> GetParaIds(JsonElement ch, string theoryDir)
    {
        if (ch.TryGetProperty("paragraphs", out var metaParas) && metaParas.ValueKind == JsonValueKind.Array)
            return metaParas.EnumerateArray()
                .Select(p => p.GetString()!)
                .Where(id => !string.IsNullOrEmpty(id));

        if (!Directory.Exists(theoryDir)) return [];

        return Directory.GetFiles(theoryDir, "*.html")
            .Where(f => !Path.GetFileName(f).Equals("index.html", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .Select(f => Path.GetFileNameWithoutExtension(f)!);
    }

    private static object BuildParagraph(string paraId, string theoryDir, string testsDir, string examplesDir)
    {
        var title    = GetHtmlTitle(Path.Combine(theoryDir, paraId + ".html")) ?? paraId;
        var tests    = LoadParaFiles(testsDir,    paraId, "*.json");
        var examples = LoadParaFiles(examplesDir, paraId, "*.cpp");
        return new { id = paraId, title, tests, examples };
    }

    /// <summary>
    /// Загружает файлы параграфа по двум стратегиям:
    /// 1. Одиночный файл <c>{dir}/{paraId}.ext</c>
    /// 2. Директория <c>{dir}/{paraId}/*.ext</c>
    /// </summary>
    private static List<object> LoadParaFiles(string dir, string paraId, string pattern)
    {
        List<object> result = [];
        var ext    = Path.GetExtension(pattern).TrimStart('*');

        var singleFile = Path.Combine(dir, paraId + ext);
        if (File.Exists(singleFile))
        {
            var item = BuildFileItem(singleFile, ext);
            if (item is not null) result.Add(item);
            return result;
        }

        var folder = Path.Combine(dir, paraId);
        if (Directory.Exists(folder))
            foreach (var f in Directory.GetFiles(folder, pattern).OrderBy(x => x))
            {
                var item = BuildFileItem(f, ext);
                if (item is not null) result.Add(item);
            }

        return result;
    }

    /// <summary>
    /// Строит метаданные файла: для JSON читает quizId/title/type, для остальных — только имя.
    /// </summary>
    private static object? BuildFileItem(string filePath, string ext) =>
        ext == ".json" ? ReadTestMeta(filePath) : new { id = Path.GetFileNameWithoutExtension(filePath) };

    private static object? ReadTestMeta(string filePath)
    {
        try
        {
            using var doc  = JsonDocument.Parse(File.ReadAllText(filePath));
            var quizId = doc.RootElement.TryGetProperty("quizId", out var qid) ? qid.GetString() : Path.GetFileNameWithoutExtension(filePath);
            var title  = doc.RootElement.TryGetProperty("title",  out var tt)  ? tt.GetString()  : quizId;
            var type   = doc.RootElement.TryGetProperty("type",   out var tp)  ? tp.GetString()  : "mini";
            return new { quizId, title, type };
        }
        catch { return null; }
    }

    /// <summary>
    /// Извлекает текст первого <c>&lt;h1&gt;</c> из HTML-файла параграфа.
    /// Возвращает <c>null</c>, если файл не найден или тег отсутствует.
    /// </summary>
    private static string? GetHtmlTitle(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var html = File.ReadAllText(filePath);
        var m = System.Text.RegularExpressions.Regex.Match(
            html, @"<h1[^>]*>(.*?)</h1>",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        if (!m.Success) return null;
        return System.Text.RegularExpressions.Regex.Replace(m.Groups[1].Value, "<[^>]+>", "").Trim();
    }
}
