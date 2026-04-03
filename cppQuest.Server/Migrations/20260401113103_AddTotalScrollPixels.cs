using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cppQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalScrollPixels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TotalScrollPixels",
                table: "GamificationProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalScrollPixels",
                table: "GamificationProfiles");
        }
    }
}
