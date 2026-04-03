using System.Text.Json;

namespace cppQuest.Server.Endpoints;

public static class CourseEndpoints
{
    public static void MapCourseEndpoints(this IEndpointRouteBuilder app, string cppCoursePath)
    {
        var api = app.MapGroup("/api");
        api.MapGet("examples/{**path}",  (string path, IWebHostEnvironment env) => GetExample(path, env));
        api.MapGet("tests/{**path}",     (string path, IWebHostEnvironment env) => GetTest(path, env));
        api.MapGet("course-structure",   (IWebHostEnvironment env)               => GetCourseStructure(cppCoursePath, env));
    }

    // GET /api/examples/{**path}
    private static IResult GetExample(string path, IWebHostEnvironment env)
    {
        if (path.Contains("..")) return Results.BadRequest();
        var root     = Path.GetFullPath(Path.Combine(env.ContentRootPath, "examples"));
        var filePath = Path.GetFullPath(Path.Combine(root, path));
        if (!filePath.StartsWith(root)) return Results.BadRequest();
        if (!File.Exists(filePath))     return Results.NotFound();
        return Results.Ok(new { path, code = File.ReadAllText(filePath) });
    }

    // GET /api/tests/{**path}
    private static IResult GetTest(string path, IWebHostEnvironment env)
    {
        if (path.Contains("..")) return Results.BadRequest();
        var root     = Path.GetFullPath(Path.Combine(env.ContentRootPath, "tests"));
        var filePath = Path.GetFullPath(Path.Combine(root, path));
        if (!filePath.StartsWith(root)) return Results.BadRequest();
        if (!File.Exists(filePath))     return Results.NotFound();
        return Results.Content(File.ReadAllText(filePath), "application/json");
    }

    // GET /api/course-structure
    private static IResult GetCourseStructure(string cppCoursePath, IWebHostEnvironment env)
    {
        var metaPath = Path.Combine(cppCoursePath, "course-meta.json");
        if (!File.Exists(metaPath))
            return Results.NotFound(new { message = "course-meta.json not found" });

        using var meta = JsonDocument.Parse(File.ReadAllText(metaPath));
        var testsRoot  = Path.Combine(env.ContentRootPath, "tests");
        var theoryRoot = Path.Combine(cppCoursePath, "theory");

        var chapters = meta.RootElement.GetProperty("chapters").EnumerateArray()
            .Select(ch => BuildChapter(ch, theoryRoot, testsRoot, env.ContentRootPath))
            .ToList();

        return Results.Ok(new { chapters });
    }

    private static object BuildChapter(JsonElement ch, string theoryRoot, string testsRoot, string contentRoot)
    {
        var chapterId = ch.GetProperty("id").GetString()!;
        var groupId   = ch.GetProperty("groupId").GetString()!;
        var title     = ch.GetProperty("title").GetString();
        var desc      = ch.TryGetProperty("description", out var d)  ? d.GetString()   : null;
        var chNum     = ch.TryGetProperty("number",      out var num) ? num.GetInt32()  : 0;

        var theoryDir     = Path.Combine(theoryRoot, chapterId, groupId);
        var chTestsDir    = Path.Combine(testsRoot,  chapterId, groupId);
        var chExamplesDir = Path.Combine(contentRoot, "examples", chapterId, groupId);

        var paraIds    = GetParaIds(ch, theoryDir);
        var paragraphs = paraIds.Select(id => BuildParagraph(id, theoryDir, chTestsDir, chExamplesDir))
                                .ToList<object>();

        return new { id = chapterId, number = chNum, groupId, title, description = desc, paragraphs };
    }

    private static IEnumerable<string> GetParaIds(JsonElement ch, string theoryDir)
    {
        if (ch.TryGetProperty("paragraphs", out var metaParas) && metaParas.ValueKind == JsonValueKind.Array)
            return metaParas.EnumerateArray()
                .Select(p => p.GetString()!)
                .Where(id => !string.IsNullOrEmpty(id));

        if (!Directory.Exists(theoryDir)) return Enumerable.Empty<string>();
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

    // Loads files for a paragraph: {dir}/{paraId}.ext  OR  {dir}/{paraId}/*.ext
    private static List<object> LoadParaFiles(string dir, string paraId, string pattern)
    {
        var result = new List<object>();
        var ext    = Path.GetExtension(pattern).TrimStart('*');

        var singleFile = Path.Combine(dir, paraId + ext);
        if (File.Exists(singleFile))
        {
            var item = ext == ".json" ? ReadTestMeta(singleFile) : (object)new { id = Path.GetFileNameWithoutExtension(singleFile) };
            if (item != null) result.Add(item);
            return result;
        }

        var folder = Path.Combine(dir, paraId);
        if (Directory.Exists(folder))
            foreach (var f in Directory.GetFiles(folder, pattern).OrderBy(x => x))
            {
                var item = ext == ".json" ? ReadTestMeta(f) : (object)new { id = Path.GetFileNameWithoutExtension(f) };
                if (item != null) result.Add(item);
            }

        return result;
    }

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

    private static string? GetHtmlTitle(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var html = File.ReadAllText(filePath);
        var m = System.Text.RegularExpressions.Regex.Match(html, @"<h1[^>]*>(.*?)</h1>", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (!m.Success) return null;
        return System.Text.RegularExpressions.Regex.Replace(m.Groups[1].Value, "<[^>]+>", "").Trim();
    }
}
