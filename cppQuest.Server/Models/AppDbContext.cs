using Microsoft.EntityFrameworkCore;

namespace cppQuest.Server.Models;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // GamificationProfile — 1:1 с User, PK = UserId
        modelBuilder.Entity<GamificationProfile>()
            .HasKey(g => g.UserId);
        modelBuilder.Entity<GamificationProfile>()
            .HasOne(g => g.User)
            .WithOne(u => u.GamificationProfile)
            .HasForeignKey<GamificationProfile>(g => g.UserId);

        // ParagraphReadingStats — составной PK (UserId, ParagraphId)
        modelBuilder.Entity<ParagraphReadingStats>()
            .HasKey(r => new { r.UserId, r.ParagraphId });
        modelBuilder.Entity<ParagraphReadingStats>()
            .HasOne(r => r.User)
            .WithMany(u => u.ReadingStats)
            .HasForeignKey(r => r.UserId);

        // ParagraphTestStats — составной PK (UserId, TestId)
        modelBuilder.Entity<ParagraphTestStats>()
            .HasKey(t => new { t.UserId, t.TestId });
        modelBuilder.Entity<ParagraphTestStats>()
            .HasOne(t => t.User)
            .WithMany(u => u.TestStats)
            .HasForeignKey(t => t.UserId);

        // TestAttempt — индекс для быстрого поиска по (UserId, TestId)
        modelBuilder.Entity<TestAttempt>()
            .HasIndex(a => new { a.UserId, a.TestId });
        modelBuilder.Entity<TestAttempt>()
            .HasOne(a => a.User)
            .WithMany(u => u.TestAttempts)
            .HasForeignKey(a => a.UserId);

        // ActivityLog — индекс по UserId
        modelBuilder.Entity<ActivityLog>()
            .HasIndex(l => l.UserId);
        modelBuilder.Entity<ActivityLog>()
            .HasOne(l => l.User)
            .WithMany(u => u.ActivityLogs)
            .HasForeignKey(l => l.UserId);

        // ShopItem — PK строковый
        modelBuilder.Entity<ShopItem>()
            .HasKey(s => s.Id);

        // UserPurchase — уникальный индекс (UserId, ItemId)
        modelBuilder.Entity<UserPurchase>()
            .HasIndex(p => new { p.UserId, p.ItemId })
            .IsUnique();
        modelBuilder.Entity<UserPurchase>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId);
        modelBuilder.Entity<UserPurchase>()
            .HasOne(p => p.Item)
            .WithMany(i => i.Purchases)
            .HasForeignKey(p => p.ItemId);

        // ParagraphSlotPurchase — уникальный индекс (UserId, Page, Slot, Std)
        modelBuilder.Entity<ParagraphSlotPurchase>()
            .HasIndex(p => new { p.UserId, p.Page, p.Slot, p.Std })
            .IsUnique();
        modelBuilder.Entity<ParagraphSlotPurchase>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId);

        // QuestionProgress — составной PK (UserId, TestId, QuestionId)
        modelBuilder.Entity<QuestionProgress>()
            .HasKey(q => new { q.UserId, q.TestId, q.QuestionId });
        modelBuilder.Entity<QuestionProgress>()
            .HasOne(q => q.User)
            .WithMany(u => u.QuestionProgress)
            .HasForeignKey(q => q.UserId);
        // Индекс для быстрого поиска вопросов к повтору
        modelBuilder.Entity<QuestionProgress>()
            .HasIndex(q => new { q.UserId, q.NextDueAt });
        modelBuilder.Entity<QuestionProgress>()
            .HasIndex(q => new { q.UserId, q.TestId });

        // UserAchievement — составной PK (UserId, AchievementId)
        modelBuilder.Entity<UserAchievement>()
            .HasKey(a => new { a.UserId, a.AchievementId });
        modelBuilder.Entity<UserAchievement>()
            .HasOne(a => a.User)
            .WithMany(u => u.Achievements)
            .HasForeignKey(a => a.UserId);

        // Seed: стандарты C++
        modelBuilder.Entity<ShopItem>().HasData(
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
