using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace cppQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllowedIsus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsuNumber = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowedIsus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Emoji = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ItemType = table.Column<string>(type: "TEXT", nullable: false),
                    CostCoins = table.Column<int>(type: "INTEGER", nullable: false),
                    CostKeys = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredStd = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    IsuNumber = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", nullable: false),
                    ParagraphId = table.Column<string>(type: "TEXT", nullable: false),
                    TestId = table.Column<string>(type: "TEXT", nullable: false),
                    XpEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    CoinsEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeSpent = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GamificationProfiles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Xp = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Coins = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentStreak = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxStreak = table.Column<int>(type: "INTEGER", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FreezeCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SnowflakeCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalStreakDays = table.Column<int>(type: "INTEGER", nullable: false),
                    Keys = table.Column<int>(type: "INTEGER", nullable: false),
                    UnlockedContentStds = table.Column<string>(type: "TEXT", nullable: false),
                    UnlockedSlots = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamificationProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_GamificationProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PageFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    PageId = table.Column<string>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageFeedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ParagraphReadingStats",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParagraphId = table.Column<string>(type: "TEXT", nullable: false),
                    TotalReadingSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    ReadingSessionsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CodeRunsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SrInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    SrEaseFactor = table.Column<float>(type: "REAL", nullable: false),
                    SrRepetitions = table.Column<int>(type: "INTEGER", nullable: false),
                    SrNextDue = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParagraphReadingStats", x => new { x.UserId, x.ParagraphId });
                    table.ForeignKey(
                        name: "FK_ParagraphReadingStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParagraphSlotPurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Page = table.Column<string>(type: "TEXT", nullable: false),
                    Slot = table.Column<string>(type: "TEXT", nullable: false),
                    Std = table.Column<string>(type: "TEXT", nullable: false),
                    CostCoins = table.Column<int>(type: "INTEGER", nullable: false),
                    CostKeys = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParagraphSlotPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParagraphSlotPurchases_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParagraphTestStats",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    TestId = table.Column<string>(type: "TEXT", nullable: false),
                    ParagraphId = table.Column<string>(type: "TEXT", nullable: false),
                    TestTitle = table.Column<string>(type: "TEXT", nullable: false),
                    AttemptsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BestScore = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalScoreSum = table.Column<int>(type: "INTEGER", nullable: false),
                    LastScore = table.Column<int>(type: "INTEGER", nullable: false),
                    BestStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParagraphTestStats", x => new { x.UserId, x.TestId });
                    table.ForeignKey(
                        name: "FK_ParagraphTestStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionProgress",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    TestId = table.Column<string>(type: "TEXT", nullable: false),
                    QuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParagraphId = table.Column<string>(type: "TEXT", nullable: false),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    CorrectStreak = table.Column<int>(type: "INTEGER", nullable: false),
                    SrInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextDueAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionProgress", x => new { x.UserId, x.TestId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_QuestionProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParagraphId = table.Column<string>(type: "TEXT", nullable: false),
                    TestId = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalQuestions = table.Column<int>(type: "INTEGER", nullable: false),
                    WrongQuestionIds = table.Column<string>(type: "TEXT", nullable: false),
                    CorrectQuestionIds = table.Column<string>(type: "TEXT", nullable: false),
                    TimeSpent = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPurchases_ShopItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ShopItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPurchases_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ShopItems",
                columns: new[] { "Id", "CostCoins", "CostKeys", "Description", "Emoji", "ItemType", "Name", "RequiredLevel", "RequiredStd" },
                values: new object[,]
                {
                    { "std_cpp11", 500, 0, "Лямбды, `auto`, `nullptr`, `range-for`, `std::thread`, move-семантика", "⚡", "std_unlock", "C++11", 2, null },
                    { "std_cpp14", 20000, 0, "`auto` в лямбдах, бинарные литералы, relaxed `constexpr`", "💧", "std_unlock", "C++14", 7, null },
                    { "std_cpp17", 40000, 0, "`if constexpr`, structured bindings, `std::optional`, `std::variant`", "🌪️", "std_unlock", "C++17", 14, null },
                    { "std_cpp20", 80000, 0, "Concepts, Ranges, Coroutines, `std::format`", "🔥", "std_unlock", "C++20", 21, null },
                    { "std_cpp23", 100000, 0, "`std::print`, `std::expected`, deducing this, `flat_map`", "🌿", "std_unlock", "C++23", 28, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId",
                table: "ActivityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PageFeedbacks_UserId",
                table: "PageFeedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParagraphSlotPurchases_UserId_Page_Slot_Std",
                table: "ParagraphSlotPurchases",
                columns: new[] { "UserId", "Page", "Slot", "Std" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionProgress_UserId_NextDueAt",
                table: "QuestionProgress",
                columns: new[] { "UserId", "NextDueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionProgress_UserId_TestId",
                table: "QuestionProgress",
                columns: new[] { "UserId", "TestId" });

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_UserId_TestId",
                table: "TestAttempts",
                columns: new[] { "UserId", "TestId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPurchases_ItemId",
                table: "UserPurchases",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPurchases_UserId_ItemId",
                table: "UserPurchases",
                columns: new[] { "UserId", "ItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "AllowedIsus");

            migrationBuilder.DropTable(
                name: "GamificationProfiles");

            migrationBuilder.DropTable(
                name: "PageFeedbacks");

            migrationBuilder.DropTable(
                name: "ParagraphReadingStats");

            migrationBuilder.DropTable(
                name: "ParagraphSlotPurchases");

            migrationBuilder.DropTable(
                name: "ParagraphTestStats");

            migrationBuilder.DropTable(
                name: "QuestionProgress");

            migrationBuilder.DropTable(
                name: "TestAttempts");

            migrationBuilder.DropTable(
                name: "UserPurchases");

            migrationBuilder.DropTable(
                name: "ShopItems");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
