using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using cppQuest.Server.Models;

namespace cppQuest.Server.Repositories;

public class UserRepository(AppDbContext db, IWebHostEnvironment env) : IUserRepository
{
    private static HashSet<string>? _allowedCache;
    private static DateTime _allowedCacheTime;

    private HashSet<string> LoadAllowedIsus()
    {
        // Перечитываем файл не чаще раза в минуту
        if (_allowedCache != null && (DateTime.UtcNow - _allowedCacheTime).TotalSeconds < 60)
            return _allowedCache;

        var path = Path.Combine(env.ContentRootPath, "allowed-isu.json");
        if (!File.Exists(path))
        {
            _allowedCache = [];
            _allowedCacheTime = DateTime.UtcNow;
            return _allowedCache;
        }

        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            _allowedCache = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            _allowedCache = [];
        }
        _allowedCacheTime = DateTime.UtcNow;
        return _allowedCache;
    }

    public async Task<User?> GetByIsuNumberAsync(string isuNumber) =>
        await db.Users.FirstOrDefaultAsync(u => u.IsuNumber == isuNumber);

    public async Task<User?> GetByIdAsync(int id) =>
        await db.Users.FindAsync(id);

    public Task<bool> IsIsuAllowedAsync(string isuNumber) =>
        Task.FromResult(LoadAllowedIsus().Contains(isuNumber));

    public async Task AddAsync(User user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }
}
