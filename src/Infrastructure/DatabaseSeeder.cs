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
        var existingKeys = (from p in db.ApiProviders
                            select new { p.Name, p.Capability, p.Model }).ToList();

        var toAdd = new List<ApiProvider>
        {
            // ═══ DeepSeek ═══
            new() { Name = "DeepSeek (DeepSeek-R1)",       Capability = AiCapability.TextToText, Type = ProviderType.LLM,      ApiUrl = "https://api.deepseek.com/v1",        ApiKey = "", Model = "deepseek-v4-flash",    CreatedTime = now, UpdatedTime = now },

            // ═══ Qwen (通义千问) ═══
            new() { Name = "Qwen (通义千问)",              Capability = AiCapability.TextToText, Type = ProviderType.LLM,      ApiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",  ApiKey = "", Model = "qwen-plus",            CreatedTime = now, UpdatedTime = now },

            // ═══ Gemini (Google) ═══
            new() { Name = "Gemini (Google)",              Capability = AiCapability.TextToText, Type = ProviderType.LLM,      ApiUrl = "https://generativelanguage.googleapis.com/v1beta/openai/",  ApiKey = "", Model = "gemini-2.0-flash",  CreatedTime = now, UpdatedTime = now },

            // ═══ Suno AI (音乐生成) ═══
            new() { Name = "Suno AI",                      Capability = AiCapability.TextToMusic, Type = ProviderType.LLM,      ApiUrl = "https://api.suno.ai/v1",             ApiKey = "", Model = "suno-v3.5",            CreatedTime = now, UpdatedTime = now },

            // ═══ Kling AI (文生视频) ═══
            new() { Name = "Kling AI (快手可灵)",           Capability = AiCapability.TextToVideo, Type = ProviderType.LLM,      ApiUrl = "https://api.klingai.com/v1",         ApiKey = "", Model = "kling-v1-6",           CreatedTime = now, UpdatedTime = now },

            // ═══ ComfyUI (Local) - 每个工作流独立记录 ═══
            new() { Name = "ComfyUI (Local) - 文生图",     Capability = AiCapability.TextToImage, Type = ProviderType.ComfyUI,  ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "01.ZIMAGE-text-to-image.json",  CreatedTime = now, UpdatedTime = now },
            new() { Name = "ComfyUI (Local) - 人物档案",   Capability = AiCapability.ImageEdit, Type = ProviderType.ComfyUI,  ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "02.ZIMAGE-character-profile.json", CreatedTime = now, UpdatedTime = now },
            new() { Name = "ComfyUI (Local) - 文生视频",   Capability = AiCapability.TextToVideo, Type = ProviderType.ComfyUI,  ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "03.LTX-text-to-video.json",     CreatedTime = now, UpdatedTime = now },
            new() { Name = "ComfyUI (Local) - 图生视频",   Capability = AiCapability.ImageToVideo, Type = ProviderType.ComfyUI, ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "04.LTX-image-to-video.json",    CreatedTime = now, UpdatedTime = now },
            new() { Name = "ComfyUI (Local) - 分镜生成",   Capability = AiCapability.ImageEdit, Type = ProviderType.ComfyUI,  ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "05.Hidream-storyboard.json",    CreatedTime = now, UpdatedTime = now },
            new() { Name = "ComfyUI (Local) - 音乐生成",   Capability = AiCapability.TextToMusic, Type = ProviderType.ComfyUI,  ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "06.ACE-music-compose.json",     CreatedTime = now, UpdatedTime = now },
            new() { Name = "ComfyUI (Local) - BGM生成",    Capability = AiCapability.TextToAudio, Type = ProviderType.ComfyUI,  ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "07.Stable-bgm-generate.json",   CreatedTime = now, UpdatedTime = now },
            new() { Name = "ComfyUI (Local) - LLM对话",    Capability = AiCapability.TextToText, Type = ProviderType.ComfyUI,  ApiUrl = "http://localhost:8188",              ApiKey = "", Model = "08.LLM-QWen.json",            CreatedTime = now, UpdatedTime = now },
        };

        var toInsert = toAdd.Where(p => !existingKeys.Any(e => e.Name == p.Name && e.Capability == p.Capability && e.Model == p.Model)).ToList();

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
