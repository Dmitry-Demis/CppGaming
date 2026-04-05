using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cppQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBankMasteryXpAwarded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BankMasteryXpAwarded",
                table: "ParagraphTestStats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankMasteryXpAwarded",
                table: "ParagraphTestStats");
        }
    }
}
