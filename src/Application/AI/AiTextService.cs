using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Application.AI;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.AI;

public interface IAiTextService
{
    Task<string> GenerateStoryAsync(string title, string prompt, long? projectId = null, CancellationToken ct = default);
    Task<string> RewriteStoryAsync(string prompt, string originalStory, long? projectId = null, CancellationToken ct = default);
    Task<string> ExtractAssetsAsync(string story, long? projectId = null, CancellationToken ct = default);
    Task<string> CreateCharacterProfileAsync(string characterDescription, long? projectId = null, CancellationToken ct = default);
    Task<string> CreateSceneProfileAsync(string sceneDescription, long? projectId = null, CancellationToken ct = default);
    Task<string> CreatePropProfileAsync(string propDescription, long? projectId = null, CancellationToken ct = default);
    Task<string> CreateSkillProfileAsync(string skillDescription, long? projectId = null, CancellationToken ct = default);
    Task<string> CreateBgmPromptAsync(string bgmDescription, long? projectId = null, CancellationToken ct = default);
    Task<string> CreateVideoPromptAsync(string dynamicDescription, List<string>? referenceImages = null, long? projectId = null, CancellationToken ct = default);
}

public class AiTextService : IAiTextService
{
    private readonly IProjectDbContext _db;
    private readonly IAiClientRegistry _registry;
    private readonly ILogger<AiTextService> _logger;

    public AiTextService(IProjectDbContext db, IAiClientRegistry registry, ILogger<AiTextService> logger)
    {
        _db = db;
        _registry = registry;
        _logger = logger;
    }

    public async Task<string> GenerateStoryAsync(string title, string prompt, long? projectId = null, CancellationToken ct = default)
    {
        var sys = await Template("StoryGeneration");
        var user = $"标题：{title}\n故事主题：{prompt}";
        return await CallTextToText(sys, user, projectId, ct);
    }

    public async Task<string> RewriteStoryAsync(string prompt, string originalStory, long? projectId = null, CancellationToken ct = default)
    {
        var sys = await Template("StoryGeneration");
        var user = $"改写要求：{prompt}\n原始故事：{originalStory}";
        return await CallTextToText(sys, user, projectId, ct);
    }

    public async Task<string> ExtractAssetsAsync(string story, long? projectId = null, CancellationToken ct = default)
    {
        var sys = await Template("EpisodeBreakdown");
        var user = $"故事内容：{story}";
        return await CallTextToText(sys, user, projectId, ct);
    }

    public async Task<string> CreateCharacterProfileAsync(string description, long? projectId = null, CancellationToken ct = default)
    {
        var sys = await Template("CharacterProfile");
        var user = $"角色描述：{description}";
        return await CallTextToText(sys, user, projectId, ct);
    }

    public async Task<string> CreateSceneProfileAsync(string description, long? projectId = null, CancellationToken ct = default)
    {
        var sys = await Template("SceneProfile");
        var user = $"场景描述：{description}";
        return await CallTextToText(sys, user, projectId, ct);
    }

    public async Task<string> CreatePropProfileAsync(string description, long? projectId = null, CancellationToken ct = default)
    {
        var sys = await Template("PropProfile");
        var user = $"道具描述：{description}";
        return await CallTextToText(sys, user, projectId, ct);
    }

    public async Task<string> CreateSkillProfileAsync(string description, long? projectId = null, CancellationToken ct = default)
    {
        var sys = await Template("ShotPlanning");
        var user = $"技能描述：{description}";
        return await CallTextToText(sys, user, projectId, ct);
    }

    public async Task<string> CreateBgmPromptAsync(string description, long? projectId = null, CancellationToken ct = default)
    {
        var sys = await Template("BgmProfile");
        var user = $"BGM描述：{description}";
        return await CallTextToText(sys, user, projectId, ct);
    }

    public async Task<string> CreateVideoPromptAsync(string dynamicDescription, List<string>? referenceImages = null, long? projectId = null, CancellationToken ct = default)
    {
        var sys = await Template("ShotPlanning");
        var user = $"动态描述：{dynamicDescription}";
        if (referenceImages?.Count > 0) user += $"\n参考图：{string.Join(", ", referenceImages)}";
        return await CallTextToText(sys, user, projectId, ct);
    }

    private async Task<string> Template(string templateType)
    {
        var t = await _db.PromptTemplates
            .Where(p => p.TemplateType == templateType && p.IsDefault)
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync();
        if (t != null)
            return t.Content;

        return await CreateTemplateAsync(templateType);
    }

    private async Task<string> CreateTemplateAsync(string templateType)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var content = templateType switch
        {
            "StoryGeneration" => @"你是一个专业的剧本创作AI，擅长将故事转化为完整的剧本。请根据用户的标题和故事主题，生成完整的剧本内容。你的输出必须是严格的JSON格式，包含以下字段：name（剧本名称）、content（故事梗概）、chapters（章节数组，每章含chapterNumber/chapterName/content）、characters（角色清单含name/description）、scenes（场景清单含name/description）、bgms（音乐清单含name/description）、props（道具清单含name/description）、skills（技能清单含name/description）。每个清单项都必须有说明。使用代码块包裹JSON输出。",
            "SystemPrompt" => @"你是一个专业的剧本创作AI。输出严格的JSON对象，包含name、content、chapters（chapterNumber/chapterName/content）、characters（name/description）、scenes（name/description）、bgms（name/description）、props（name/description）、skills（name/description），每个清单项必须有说明。",
            "CharacterProfile" => "你是一个角色设计师。根据角色描述，生成完整的角色档案，包括外貌特征、性格特点、背景故事和英文生图提示词。以JSON格式返回。",
            "SceneProfile" => "你是一个场景设计师。根据场景描述，生成详细的场景档案和英文生图提示词。以JSON格式返回。",
            "PropProfile" => "你是一个道具设计师。根据道具描述，生成详细的道具档案和英文生图提示词。以JSON格式返回。",
            "BgmProfile" => "你是一个音乐制作人。根据BGM描述，生成详细的音乐风格说明和英文音频生成提示词。以JSON格式返回。",
            "EpisodeBreakdown" => "你是一个剧本分析专家。从给定的故事内容中提取所有角色、道具、场景和BGM。每个资产需要名称和详细描述。以JSON格式返回。",
            "ShotPlanning" => "你是一个视频导演。根据动态描述生成视频生成提示词，包括运镜、氛围、画质要求。以JSON格式返回。",
            "SkillProfile" => "你是一个游戏/动画设计师。根据技能描述，生成完整的技能档案。以JSON格式返回。",
            _ => "你是一个AI助手，根据用户输入生成对应的JSON格式输出。"
        };

        await _db.PromptTemplates.AddAsync(new PromptTemplate
        {
            Name = templateType,
            TemplateType = templateType,
            IsDefault = true,
            Content = content,
            CreatedTime = now,
            UpdatedTime = now
        });
        await _db.SaveChangesAsync();
        _logger.LogInformation("自动创建默认模板 {Type}", templateType);
        return content;
    }

    private async Task<string> CallTextToText(string systemPrompt, string userContent, long? projectId, CancellationToken ct)
    {
        var provider = await FindProviderAsync(AiCapability.TextToText, projectId, ct);
        if (provider == null)
        {
            _logger.LogWarning("未找到文本到文本的 API 提供者");
            return "";
        }

        _logger.LogInformation("使用 {Name} 执行文本到文本 (Model: {Model})", provider.Name, provider.Model);

        var client = _registry.GetTextToTextClient(provider);
        if (client == null)
        {
            _logger.LogWarning("不支持的 Provider: {Name}", provider.Name);
            return "";
        }

        try
        {
            return await client.GenerateAsync(systemPrompt, userContent, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Name} 调用异常", provider.Name);
            return "";
        }
    }

    private async Task<ApiProvider?> FindProviderAsync(AiCapability capability, long? projectId, CancellationToken ct)
    {
        var query = _db.ApiProviders.Where(p => p.Capability == capability);

        if (projectId.HasValue && projectId.Value > 0)
        {
            // 先找项目级别的 provider（按名字中包含项目名称或标识）
            // 简单实现：如果没有项目级的，就返回全局
        }

        return await query.FirstOrDefaultAsync(ct);
    }
}
