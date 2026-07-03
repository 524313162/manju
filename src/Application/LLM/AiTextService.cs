using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.LLM;

public class AiTextService : IAiTextService
{
    private readonly ILLMClient _llm;
    private readonly IProjectDbContext _db;
    private readonly ILogger<AiTextService> _logger;

    public AiTextService(ILLMClient llm, IProjectDbContext db, ILogger<AiTextService> logger)
    {
        _llm = llm;
        _db = db;
        _logger = logger;
    }

    static readonly Dictionary<string, string> TypeMap = new()
    {
        ["GenerateStory"] = "StoryGeneration",
        ["RewriteStory"] = "StoryGeneration",
        ["ExtractAssets"] = "EpisodeBreakdown",
        ["CreateCharacterProfile"] = "CharacterProfile",
        ["CreateSceneProfile"] = "SceneProfile",
        ["CreatePropProfile"] = "PropProfile",
        ["CreateSkillProfile"] = "ShotPlanning",
        ["CreateBgmPrompt"] = "BgmProfile",
        ["CreateVideoPrompt"] = "ShotPlanning",
    };

    async Task<string> LoadTemplateAsync(string method)
    {
        var type = TypeMap.GetValueOrDefault(method, "StoryGeneration");
        var template = await _db.PromptTemplates
            .Where(t => t.TemplateType == type && t.IsDefault)
            .OrderBy(t => t.Id)
            .FirstOrDefaultAsync();
        if (template == null)
        {
            _logger.LogWarning("未找到默认模板 {Type}，使用内置默认", type);
            return DefaultPrompt(type);
        }
        return template.Content;
    }

    public async Task<string> GenerateStoryAsync(string title, string prompt, CancellationToken ct = default)
    {
        var sys = await LoadTemplateAsync("GenerateStory");
        return await _llm.GenerateAsync(sys, $"标题：{title}\n故事主题：{prompt}", ct);
    }

    public async Task<string> RewriteStoryAsync(string prompt, string originalStory, CancellationToken ct = default)
    {
        var sys = await LoadTemplateAsync("RewriteStory");
        return await _llm.GenerateAsync(sys, $"改写要求：{prompt}\n原始故事：{originalStory}", ct);
    }

    public async Task<string> ExtractAssetsAsync(string story, CancellationToken ct = default)
    {
        var sys = await LoadTemplateAsync("ExtractAssets");
        return await _llm.GenerateAsync(sys, $"故事内容：{story}", ct);
    }

    public async Task<string> CreateCharacterProfileAsync(string characterDescription, CancellationToken ct = default)
    {
        var sys = await LoadTemplateAsync("CreateCharacterProfile");
        return await _llm.GenerateAsync(sys, $"角色描述：{characterDescription}", ct);
    }

    public async Task<string> CreateSceneProfileAsync(string sceneDescription, CancellationToken ct = default)
    {
        var sys = await LoadTemplateAsync("CreateSceneProfile");
        return await _llm.GenerateAsync(sys, $"场景描述：{sceneDescription}", ct);
    }

    public async Task<string> CreatePropProfileAsync(string propDescription, CancellationToken ct = default)
    {
        var sys = await LoadTemplateAsync("CreatePropProfile");
        return await _llm.GenerateAsync(sys, $"道具描述：{propDescription}", ct);
    }

    public async Task<string> CreateSkillProfileAsync(string skillDescription, CancellationToken ct = default)
    {
        var sys = await LoadTemplateAsync("CreateSkillProfile");
        return await _llm.GenerateAsync(sys, $"技能描述：{skillDescription}", ct);
    }

    public async Task<string> CreateBgmPromptAsync(string bgmDescription, CancellationToken ct = default)
    {
        var sys = await LoadTemplateAsync("CreateBgmPrompt");
        return await _llm.GenerateAsync(sys, $"BGM描述：{bgmDescription}", ct);
    }

    public async Task<string> CreateVideoPromptAsync(string dynamicDescription, List<string>? referenceImages = null, CancellationToken ct = default)
    {
        var sys = await LoadTemplateAsync("CreateVideoPrompt");
        var user = $"动态描述：{dynamicDescription}";
        if (referenceImages?.Count > 0) user += $"\n参考图：{string.Join(", ", referenceImages)}";
        return await _llm.GenerateAsync(sys, user, ct);
    }

    static string DefaultPrompt(string type) => type switch
    {
        "StoryGeneration" => "你是一个专业的剧本创作助手。根据用户提供的标题和故事主题，生成包含3-4章的完整剧本大纲。以JSON格式返回。",
        "CharacterProfile" => "你是一个角色设计师。根据角色描述，生成完整的角色档案，包括外貌特征、性格特点、背景故事和英文生图提示词。以JSON格式返回。",
        "SceneProfile" => "你是一个场景设计师。根据场景描述，生成详细的场景档案和英文生图提示词。以JSON格式返回。",
        "PropProfile" => "你是一个道具设计师。根据道具描述，生成详细的道具档案和英文生图提示词。以JSON格式返回。",
        "BgmProfile" => "你是一个音乐制作人。根据BGM描述，生成详细的音乐风格说明和英文音频生成提示词。以JSON格式返回。",
        "EpisodeBreakdown" => "你是一个剧本分析专家。从给定的故事内容中提取所有角色、道具、场景和BGM。以JSON格式返回。",
        "ShotPlanning" => "你是一个视频导演。根据动态描述生成视频生成提示词，包括运镜、氛围、画质要求。以JSON格式返回。",
        _ => "你是一个AI助手，根据用户输入生成对应的JSON格式输出。"
    };
}