using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cppQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicIdAndScrollPixels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Assign unique GUIDs to existing users
            migrationBuilder.Sql(
                "UPDATE Users SET PublicId = lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))) WHERE PublicId = '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.AddColumn<long>(
                name: "ScrollPixels",
                table: "ParagraphReadingStats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ScrollPixels",
                table: "ParagraphReadingStats");
        }
    }
}
