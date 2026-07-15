using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManjuCraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShotAndShotFrame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CameraMovement",
                table: "Shots");

            migrationBuilder.AddColumn<long>(
                name: "ResourceId",
                table: "Shots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CameraMovement",
                table: "ShotFrames",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shots_ResourceId",
                table: "Shots",
                column: "ResourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shots_Resources_ResourceId",
                table: "Shots",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shots_Resources_ResourceId",
                table: "Shots");

            migrationBuilder.DropIndex(
                name: "IX_Shots_ResourceId",
                table: "Shots");

            migrationBuilder.DropColumn(
                name: "ResourceId",
                table: "Shots");

            migrationBuilder.DropColumn(
                name: "CameraMovement",
                table: "ShotFrames");

            migrationBuilder.AddColumn<string>(
                name: "CameraMovement",
                table: "Shots",
                type: "TEXT",
                maxLength: 64,
                nullable: true);
        }
    }
}
