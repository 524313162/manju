using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManjuCraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropShotAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShotAssets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShotAssets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShotId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UpdatedTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShotAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShotAssets_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShotAssets_Shots_ShotId",
                        column: x => x.ShotId,
                        principalTable: "Shots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShotAssets_AssetId",
                table: "ShotAssets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ShotAssets_ShotId_AssetId",
                table: "ShotAssets",
                columns: new[] { "ShotId", "AssetId" },
                unique: true);
        }
    }
}
