using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Models;

/// <summary>
/// Основной DB-контекст приложения.
/// Содержит конфигурацию сущностей и сиды для начального наполнения.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AllowedIsu> AllowedIsus => Set<AllowedIsu>();
    public DbSet<User> Users => Set<User>();
    public DbSet<GamificationProfile> GamificationProfiles => Set<GamificationProfile>();
    public DbSet<ParagraphReadingStats> ParagraphReadingStats => Set<ParagraphReadingStats>();
    public DbSet<ParagraphTestStats> ParagraphTestStats => Set<ParagraphTestStats>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<ShopItem> ShopItems => Set<ShopItem>();
    public DbSet<UserPurchase> UserPurchases => Set<UserPurchase>();
    public DbSet<ParagraphSlotPurchase> ParagraphSlotPurchases => Set<ParagraphSlotPurchase>();
    public DbSet<PageFeedback> PageFeedbacks => Set<PageFeedback>();
    public DbSet<QuestionProgress> QuestionProgress => Set<QuestionProgress>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();

    /// <summary>
    /// Конфигурирует модель данных — делегирует на небольшие метод-компоненты,
    /// чтобы код был читабельнее и каждая часть была ответственна за свою сущность.
    /// </summary>
    /// <param name="modelBuilder">Экземпляр <see cref="ModelBuilder"/> от EF Core.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureGamificationProfile(modelBuilder);
        ConfigureParagraphReadingStats(modelBuilder);
        ConfigureParagraphTestStats(modelBuilder);
        ConfigureTestAttemptAndActivityLog(modelBuilder);
        ConfigureShopAndPurchases(modelBuilder);
        ConfigureParagraphSlotPurchase(modelBuilder);
        ConfigureQuestionProgress(modelBuilder);
        ConfigureUserAchievement(modelBuilder);
        SeedShopItems(modelBuilder);
    }

    /// <summary>
    /// Конфигурация профиля геймификации (1:1 с User).
    /// </summary>
    private static void ConfigureGamificationProfile(ModelBuilder mb)
    {
        mb.Entity<GamificationProfile>()
            .HasKey(g => g.UserId);
        mb.Entity<GamificationProfile>()
            .HasOne(g => g.User)
            .WithOne(u => u.GamificationProfile)
            .HasForeignKey<GamificationProfile>(g => g.UserId);
    }

    /// <summary>
    /// Конфигурация статистики чтения параграфов (составной PK).
    /// </summary>
    private static void ConfigureParagraphReadingStats(ModelBuilder mb)
    {
        mb.Entity<ParagraphReadingStats>()
            .HasKey(r => new { r.UserId, r.ParagraphId });
        mb.Entity<ParagraphReadingStats>()
            .HasOne(r => r.User)
            .WithMany(u => u.ReadingStats)
            .HasForeignKey(r => r.UserId);
    }

    /// <summary>
    /// Конфигурация статистики тестов по параграфам (составной PK).
    /// </summary>
    private static void ConfigureParagraphTestStats(ModelBuilder mb)
    {
        mb.Entity<ParagraphTestStats>()
            .HasKey(t => new { t.UserId, t.TestId });
        mb.Entity<ParagraphTestStats>()
            .HasOne(t => t.User)
            .WithMany(u => u.TestStats)
            .HasForeignKey(t => t.UserId);
    }

    /// <summary>
    /// Индексы и связи для попыток тестов и логов активности.
    /// </summary>
    private static void ConfigureTestAttemptAndActivityLog(ModelBuilder mb)
    {
        // Попытки тестов: индекс для быстрого поиска по пользователю и тесту
        mb.Entity<TestAttempt>()
            .HasIndex(a => new { a.UserId, a.TestId });
        mb.Entity<TestAttempt>()
            .HasOne(a => a.User)
            .WithMany(u => u.TestAttempts)
            .HasForeignKey(a => a.UserId);

        // Логи активности: индекс по UserId
        mb.Entity<ActivityLog>()
            .HasIndex(l => l.UserId);
        mb.Entity<ActivityLog>()
            .HasOne(l => l.User)
            .WithMany(u => u.ActivityLogs)
            .HasForeignKey(l => l.UserId);
    }

    /// <summary>
    /// Конфигурация магазина и покупок/предметов.
    /// </summary>
    private static void ConfigureShopAndPurchases(ModelBuilder mb)
    {
        // ShopItem: строковый PK
        mb.Entity<ShopItem>()
            .HasKey(s => s.Id);

        // UserPurchase: уникальный индекс (UserId, ItemId)
        mb.Entity<UserPurchase>()
            .HasIndex(p => new { p.UserId, p.ItemId })
            .IsUnique();
        mb.Entity<UserPurchase>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId);
        mb.Entity<UserPurchase>()
            .HasOne(p => p.Item)
            .WithMany(i => i.Purchases)
            .HasForeignKey(p => p.ItemId);
    }

    /// <summary>
    /// Конфигурация покупок слотов параграфа (уникальный индекс по полям).
    /// </summary>
    private static void ConfigureParagraphSlotPurchase(ModelBuilder mb)
    {
        mb.Entity<ParagraphSlotPurchase>()
            .HasIndex(p => new { p.UserId, p.Page, p.Slot, p.Std })
            .IsUnique();
        mb.Entity<ParagraphSlotPurchase>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId);
    }

    /// <summary>
    /// Конфигурация прогресса по вопросам — составной PK и индексы.
    /// </summary>
    private static void ConfigureQuestionProgress(ModelBuilder mb)
    {
        mb.Entity<QuestionProgress>()
            .HasKey(q => new { q.UserId, q.TestId, q.QuestionId });
        mb.Entity<QuestionProgress>()
            .HasOne(q => q.User)
            .WithMany(u => u.QuestionProgress)
            .HasForeignKey(q => q.UserId);

        // Индексы для быстрых выборок вопросов к повтору
        mb.Entity<QuestionProgress>()
            .HasIndex(q => new { q.UserId, q.NextDueAt });
        mb.Entity<QuestionProgress>()
            .HasIndex(q => new { q.UserId, q.TestId });
    }

    /// <summary>
    /// Конфигурация достижений пользователя (составной ключ).
    /// </summary>
    private static void ConfigureUserAchievement(ModelBuilder mb)
    {
        mb.Entity<UserAchievement>()
            .HasKey(a => new { a.UserId, a.AchievementId });
        mb.Entity<UserAchievement>()
            .HasOne(a => a.User)
            .WithMany(u => u.Achievements)
            .HasForeignKey(a => a.UserId);
    }

    /// <summary>
    /// Сид данных для магазина — стандарты C++.
    /// Вынесено в отдельный метод ради читабельности.
    /// </summary>
    /// <param name="mb">Экземпляр <see cref="ModelBuilder"/>.</param>
    private static void SeedShopItems(ModelBuilder mb)
    {
        mb.Entity<ShopItem>().HasData(
            new ShopItem { Id = "std_cpp11", Emoji = "⚡", Name = "C++11", CostCoins = 500,    RequiredLevel = 2,  ItemType = "std_unlock",
                Description = "Лямбды, `auto`, `nullptr`, `range-for`, `std::thread`, move-семантика" },
            new ShopItem { Id = "std_cpp14", Emoji = "💧", Name = "C++14", CostCoins = 20000,  RequiredLevel = 7,  ItemType = "std_unlock",
                Description = "`auto` в лямбдах, бинарные литералы, relaxed `constexpr`" },
            new ShopItem { Id = "std_cpp17", Emoji = "🌪️", Name = "C++17", CostCoins = 40000,  RequiredLevel = 14, ItemType = "std_unlock",
                Description = "`if constexpr`, structured bindings, `std::optional`, `std::variant`" },
            new ShopItem { Id = "std_cpp20", Emoji = "🔥", Name = "C++20", CostCoins = 80000,  RequiredLevel = 21, ItemType = "std_unlock",
                Description = "Concepts, Ranges, Coroutines, `std::format`" },
            new ShopItem { Id = "std_cpp23", Emoji = "🌿", Name = "C++23", CostCoins = 100000, RequiredLevel = 28, ItemType = "std_unlock",
                Description = "`std::print`, `std::expected`, deducing this, `flat_map`" }
        );
    }
}
