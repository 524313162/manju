using System.Reflection;
using ManjuCraft.Domain.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ManjuCraft.Infrastructure;

public static class DatabaseSeeder
{
    private const string EmbeddedResourcePrefix = "ManjuCraft.Infrastructure.SeedData.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task SeedAsync(ProjectDbContext db)
    {
        var hasApiProviders = db.ApiProviders.Any();

        if (!hasApiProviders)
        {
            await SeedApiProvidersAsync(db);
        }

        await SeedPromptTemplatesAsync(db);

        var hasChanges = db.ChangeTracker.Entries().Any(e => e.State == EntityState.Added);
        if (hasChanges)
        {
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedApiProvidersAsync(ProjectDbContext db)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // Only seed if deepseek doesn't exist (most specific check)
        var existingNames = db.ApiProviders.Select(p => p.Name).ToList();

        List<string> newNames = new(); // Track for logging if needed

        var toAdd = new List<ApiProvider>
        {
            // ═══ DeepSeek ═══
            new() { Name = "DeepSeek",                    Capability = AiCapability.TextToText, ApiUrl = "https://api.deepseek.com/v1",        ApiKey = "", Model = "deepseek-v4-flash",    CreatedTime = now, UpdatedTime = now },

            // ═══ Qwen (通义千问) ═══
            new() { Name = "Qwen (通义千问)",             Capability = AiCapability.TextToText, ApiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",  ApiKey = "", Model = "qwen-plus",            CreatedTime = now, UpdatedTime = now },

            // ═══ Gemini (Google) ═══
            new() { Name = "Gemini (Google)",             Capability = AiCapability.TextToText, ApiUrl = "https://generativelanguage.googleapis.com/v1beta/openai/",  ApiKey = "", Model = "gemini-2.0-flash",  CreatedTime = now, UpdatedTime = now },

            // ═══ Suno AI (音乐生成) ═══
            new() { Name = "Suno AI",                     Capability = AiCapability.TextToMusic, ApiUrl = "https://api.suno.ai/v1",             ApiKey = "", Model = "suno-v3.5",            CreatedTime = now, UpdatedTime = now },

            // ═══ Kling AI (文生视频) ═══
            new() { Name = "Kling AI",                    Capability = AiCapability.TextToVideo, ApiUrl = "https://api.klingai.com/v1",         ApiKey = "", Model = "kling-v1-6",           CreatedTime = now, UpdatedTime = now },

            // ═══ ComfyUI (Local) - 8 个功能端点，每个端点对应一个 ApiProvider 记录 ═══
            // 01. 文生图 (ZIMAGE)
            new() { Name = "ComfyUI (Local)",             Capability = AiCapability.TextToImage, ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "01.ZIMAGE-text-to-image.json",  CreatedTime = now, UpdatedTime = now },
            // 02. 人物档案 (ZIMAGE)
            new() { Name = "ComfyUI (Local)",             Capability = AiCapability.ImageEdit, ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "02.ZIMAGE-character-profile.json", CreatedTime = now, UpdatedTime = now },
            // 03. 文生视频 (LTX)
            new() { Name = "ComfyUI (Local)",             Capability = AiCapability.TextToVideo, ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "03.LTX-text-to-video.json",     CreatedTime = now, UpdatedTime = now },
            // 04. 图生视频 (LTX)
            new() { Name = "ComfyUI (Local)",             Capability = AiCapability.ImageToVideo, ApiUrl = "http://localhost:8188",             ApiKey = "", Model = "04.LTX-image-to-video.json",    CreatedTime = now, UpdatedTime = now },
            // 05. 分镜生成 (HiDream)
            new() { Name = "ComfyUI (Local)",             Capability = AiCapability.ImageEdit, ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "05.Hidream-storyboard.json",    CreatedTime = now, UpdatedTime = now },
            // 06. 音乐生成 (ACE-MUSIC)
            new() { Name = "ComfyUI (Local)",             Capability = AiCapability.TextToMusic, ApiUrl = "http://localhost:8188",             ApiKey = "", Model = "06.ACE-music-compose.json",     CreatedTime = now, UpdatedTime = now },
            // 07. BGM 生成 (Stable-BGM)
            new() { Name = "ComfyUI (Local)",             Capability = AiCapability.TextToAudio, ApiUrl = "http://localhost:8188",             ApiKey = "", Model = "07.Stable-bgm-generate.json",   CreatedTime = now, UpdatedTime = now },
            // 08. 大语言模型 (LLM-QWen)
            new() { Name = "ComfyUI (Local)",             Capability = AiCapability.ComfyUI, ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "08.LLM-QWen.json",            CreatedTime = now, UpdatedTime = now },
        };

        var toInsert = toAdd.Where(p => !existingNames.Contains(p.Name)).ToList();

        if (toInsert.Any())
        {
            await db.ApiProviders.AddRangeAsync(toInsert);
        }
    }

    private static async Task SeedPromptTemplatesAsync(ProjectDbContext db)
    {
        var existingTypes = await db.PromptTemplates.Select(t => t.TemplateType).ToHashSetAsync();
        var templates = LoadPromptTemplatesFromEmbeddedResources();
        
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        // 更新已存在的模板（如果种子中有），插入不存在的
        foreach (var seedTemplate in templates)
        {
            var existing = await db.PromptTemplates.FirstOrDefaultAsync(t => t.TemplateType == seedTemplate.TemplateType);
            if (existing != null)
            {
                // 更新已有模板的内容
                existing.Name = seedTemplate.Name;
                existing.Content = seedTemplate.Content;
                existing.UpdatedTime = now;
            }
            else
            {
                seedTemplate.CreatedTime = now;
                seedTemplate.UpdatedTime = now;
                await db.PromptTemplates.AddAsync(seedTemplate);
            }
        }
    }

    private static List<PromptTemplate> LoadPromptTemplatesFromEmbeddedResources()
    {
        var result = new List<PromptTemplate>();
        var assembly = typeof(DatabaseSeeder).GetTypeInfo().Assembly;
        var resourceNames = assembly.GetManifestResourceNames();

        var jsonFiles = resourceNames
            .Where(r => r.StartsWith(EmbeddedResourcePrefix) && r.EndsWith(".json"))
            .ToList();

        foreach (var resourceName in jsonFiles)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.GetProperty("name").GetString();
            var templateType = root.GetProperty("templateType").GetString();
            var content = root.GetProperty("content").GetString();

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(templateType) && !string.IsNullOrEmpty(content))
            {
                result.Add(new PromptTemplate
                {
                    Name = name,
                    TemplateType = templateType,
                    Content = content
                });
            }
        }

        return result;
    }
}
