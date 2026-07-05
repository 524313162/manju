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
            new() { Name = "DeepSeek",                        Capability = AiCapability.TextToText,  ApiUrl = "https://api.deepseek.com/v1",                              ApiKey = "", Model = "deepseek-v4-flash",                          CreatedTime = now, UpdatedTime = now },
            new() { Name = "Qwen (通义千问)",                  Capability = AiCapability.TextToText,  ApiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",         ApiKey = "", Model = "qwen-plus",                                CreatedTime = now, UpdatedTime = now },
            new() { Name = "GPT-4o (OpenAI)",                  Capability = AiCapability.TextToText,  ApiUrl = "https://api.openai.com/v1",                                 ApiKey = "", Model = "gpt-4o",                                   CreatedTime = now, UpdatedTime = now },
            new() { Name = "Claude (Anthropic)",               Capability = AiCapability.TextToText,  ApiUrl = "https://api.anthropic.com/v1",                              ApiKey = "", Model = "claude-sonnet-4-20250514",                 CreatedTime = now, UpdatedTime = now },
            new() { Name = "Gemini (Google)",                  Capability = AiCapability.TextToText,  ApiUrl = "https://generativelanguage.googleapis.com/v1beta/openai/",  ApiKey = "", Model = "gemini-2.0-flash",                       CreatedTime = now, UpdatedTime = now },
            new() { Name = "Midjourney",                       Capability = AiCapability.TextToImage, ApiUrl = "https://api.midjourney.com/v1",                             ApiKey = "", Model = "midjourney-model-preview",                CreatedTime = now, UpdatedTime = now },
            new() { Name = "DALL-E 3",                        Capability = AiCapability.TextToImage, ApiUrl = "https://api.openai.com/v1/images/generations",              ApiKey = "", Model = "dall-e-3",                                 CreatedTime = now, UpdatedTime = now },
            new() { Name = "Firefly (Adobe)",                 Capability = AiCapability.TextToImage, ApiUrl = "https://firefly.adobe.com/api/v1",                          ApiKey = "", Model = "firefly-image-3",                         CreatedTime = now, UpdatedTime = now },
            new() { Name = "Flux (Black-Forest-Labs)",         Capability = AiCapability.TextToImage, ApiUrl = "https://api.bfl.ml/v1",                                      ApiKey = "", Model = "flux-pro-v1.1",                            CreatedTime = now, UpdatedTime = now },
            new() { Name = "Stable Diffusion XL",             Capability = AiCapability.TextToImage, ApiUrl = "https://api.stability.ai/v1/generation",                    ApiKey = "", Model = "stable-diffusion-xl-1024-v1-0",         CreatedTime = now, UpdatedTime = now },
            new() { Name = "Suno AI",                         Capability = AiCapability.TextToAudio, ApiUrl = "https://api.suno.ai/v1",                                    ApiKey = "", Model = "suno-v3.5",                                CreatedTime = now, UpdatedTime = now },
            new() { Name = "Udio",                            Capability = AiCapability.TextToAudio, ApiUrl = "https://api.udio.com/v1",                                   ApiKey = "", Model = "udio-v1",                                  CreatedTime = now, UpdatedTime = now },
            new() { Name = "Kling AI",                        Capability = AiCapability.TextToVideo, ApiUrl = "https://api.klingai.com/v1",                                ApiKey = "", Model = "kling-v1-6",                             CreatedTime = now, UpdatedTime = now },
            new() { Name = "Runway Gen-3",                    Capability = AiCapability.TextToVideo, ApiUrl = "https://api.runwayml.com/v1/generation",                    ApiKey = "", Model = "gen3a_turbo",                              CreatedTime = now, UpdatedTime = now },
            new() { Name = "Pika Labs",                       Capability = AiCapability.TextToVideo, ApiUrl = "https://api.pika.art/v1",                                   ApiKey = "", Model = "pika-1.0",                                 CreatedTime = now, UpdatedTime = now },
            new() { Name = "ComfyUI (Local)",                 Capability = AiCapability.TextToVideo, ApiUrl = "http://localhost:8188",                                      ApiKey = "", Model = "workflow/manjucraft-video.json",          CreatedTime = now, UpdatedTime = now },
            new() { Name = "Kling Image-to-Video",            Capability = AiCapability.ImageToVideo, ApiUrl = "https://api.klingai.com/v1",                                ApiKey = "", Model = "kling-v1-6-img2video",               CreatedTime = now, UpdatedTime = now },
            new() { Name = "Runway Image-to-Video",           Capability = AiCapability.ImageToVideo, ApiUrl = "https://api.runwayml.com/v1/generation",                    ApiKey = "", Model = "gen3a_turbo_img2video",              CreatedTime = now, UpdatedTime = now }
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
        var toInsert = templates
            .Where(t => !existingTypes.Contains(t.TemplateType))
            .ToList();

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var t in toInsert)
        {
            t.CreatedTime = now;
            t.UpdatedTime = now;
        }

        if (toInsert.Any())
        {
            await db.PromptTemplates.AddRangeAsync(toInsert);
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
