using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManjuCraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanupPromptTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete unused prompt templates (keep only SystemPrompt, RewriteStory, ShotAssetExtraction)
            migrationBuilder.Sql(@"
                DELETE FROM PromptTemplates 
                WHERE TemplateType NOT IN ('SystemPrompt', 'RewriteStory', 'ShotAssetExtraction');
            ");

            // Rename the kept templates to Chinese names
            migrationBuilder.Sql(@"
                UPDATE PromptTemplates 
                SET Name = '故事生成系统提示词', UpdatedTime = " + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + @"
                WHERE TemplateType = 'SystemPrompt';
            ");

            migrationBuilder.Sql(@"
                UPDATE PromptTemplates 
                SET Name = '剧本重写提示词', UpdatedTime = " + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + @"
                WHERE TemplateType = 'RewriteStory';
            ");

            migrationBuilder.Sql(@"
                UPDATE PromptTemplates 
                SET Name = '分镜与资产提取提示词', UpdatedTime = " + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + @"
                WHERE TemplateType = 'ShotAssetExtraction';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert names back to English
            migrationBuilder.Sql(@"
                UPDATE PromptTemplates 
                SET Name = 'System Prompt - AI Script Creation', UpdatedTime = " + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + @"
                WHERE TemplateType = 'SystemPrompt';
            ");

            migrationBuilder.Sql(@"
                UPDATE PromptTemplates 
                SET Name = 'Rewrite Story Prompt', UpdatedTime = " + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + @"
                WHERE TemplateType = 'RewriteStory';
            ");

            migrationBuilder.Sql(@"
                UPDATE PromptTemplates 
                SET Name = 'Shot & Asset Extraction Prompt', UpdatedTime = " + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + @"
                WHERE TemplateType = 'ShotAssetExtraction';
            ");
            
            // Note: Deleted templates cannot be restored via Down migration
            // They would need to be re-seeded
        }
    }
}