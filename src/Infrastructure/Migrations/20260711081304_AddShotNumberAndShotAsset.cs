using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManjuCraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShotNumberAndShotAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiProviders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Capability = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ApiUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ApiKey = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    TemplateType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaType = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    CreatedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<long>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stories_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<long>(type: "INTEGER", nullable: false),
                    ResourceId = table.Column<long>(type: "INTEGER", nullable: true),
                    AssetType = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_Assets_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Assets_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assets_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StoryChapters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StoryId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChapterNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ChapterName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Assets = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryChapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryChapters_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Episodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<long>(type: "INTEGER", nullable: false),
                    StoryChapterId = table.Column<long>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Episodes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Episodes_StoryChapters_StoryChapterId",
                        column: x => x.StoryChapterId,
                        principalTable: "StoryChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Shots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EpisodeId = table.Column<long>(type: "INTEGER", nullable: false),
                    ShotNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ShotSize = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    CameraMovement = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Duration = table.Column<float>(type: "REAL", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shots_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShotAssets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShotId = table.Column<long>(type: "INTEGER", nullable: false),
                    AssetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<long>(type: "INTEGER", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "ShotFrames",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShotId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProjectId = table.Column<long>(type: "INTEGER", nullable: false),
                    ShotNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    FrameType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ResourceId = table.Column<long>(type: "INTEGER", nullable: true),
                    StartTime = table.Column<float>(type: "REAL", nullable: true),
                    Duration = table.Column<float>(type: "REAL", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShotFrames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShotFrames_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShotFrames_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShotFrames_Shots_ShotId",
                        column: x => x.ShotId,
                        principalTable: "Shots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ParentId",
                table: "Assets",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ProjectId",
                table: "Assets",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ResourceId",
                table: "Assets",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_ProjectId",
                table: "Episodes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_StoryChapterId",
                table: "Episodes",
                column: "StoryChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Name",
                table: "Projects",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShotAssets_AssetId",
                table: "ShotAssets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ShotAssets_ShotId_AssetId",
                table: "ShotAssets",
                columns: new[] { "ShotId", "AssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShotFrames_ProjectId",
                table: "ShotFrames",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ShotFrames_ResourceId",
                table: "ShotFrames",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ShotFrames_ShotId",
                table: "ShotFrames",
                column: "ShotId");

            migrationBuilder.CreateIndex(
                name: "IX_Shots_EpisodeId",
                table: "Shots",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_ProjectId",
                table: "Stories",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryChapters_StoryId",
                table: "StoryChapters",
                column: "StoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiProviders");

            migrationBuilder.DropTable(
                name: "PromptTemplates");

            migrationBuilder.DropTable(
                name: "ShotAssets");

            migrationBuilder.DropTable(
                name: "ShotFrames");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Shots");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "Episodes");

            migrationBuilder.DropTable(
                name: "StoryChapters");

            migrationBuilder.DropTable(
                name: "Stories");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
