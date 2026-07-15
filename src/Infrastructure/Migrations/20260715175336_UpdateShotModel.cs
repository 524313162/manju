using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManjuCraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShotModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Shots");

            migrationBuilder.DropColumn(
                name: "ShotSize",
                table: "Shots");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ShotFrames",
                newName: "NarrativeDescription");

            migrationBuilder.AddColumn<string>(
                name: "GeneratePrompt",
                table: "ShotFrames",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShotSize",
                table: "ShotFrames",
                type: "TEXT",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratePrompt",
                table: "ShotFrames");

            migrationBuilder.DropColumn(
                name: "ShotSize",
                table: "ShotFrames");

            migrationBuilder.RenameColumn(
                name: "NarrativeDescription",
                table: "ShotFrames",
                newName: "Description");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Shots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShotSize",
                table: "Shots",
                type: "TEXT",
                maxLength: 32,
                nullable: true);
        }
    }
}
