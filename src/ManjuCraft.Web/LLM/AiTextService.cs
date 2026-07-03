using Microsoft.Extensions.Logging;
using ManjuCraft.Application.Service;
using ManjuCraft.Application.LLM;

namespace ManjuCraft.Web.LLM;

public class AiTextService : IAiTextService
{
    private readonly IDeepSeekService _llm;
    private readonly ILogger<AiTextService> _logger;

    public AiTextService(IDeepSeekService llm, ILogger<AiTextService> logger)
    {
        _llm = llm;
        _logger = logger;
    }

    private static readonly string StorySystemPrompt =
        "你是一个专业的剧本创作助手。根据用户提供的标题和故事主题，生成包含3-4章的完整剧本大纲。" +
        "每章需要包含：章节编号、章节名称、详细的场景描写和对话内容。" +
        "请以JSON格式返回，格式：{ \"title\": \"...\", \"chapters\": [{ \"chapterNumber\": \"...\", \"chapterName\": \"...\", \"content\": \"...\" }] }";

    private static readonly string RewriteSystemPrompt =
        "你是一个剧本改写专家。根据用户提供的改写要求，对原始故事进行二次创作。" +
        "请保留核心情节，优化情感描写、场景细节和叙事节奏。" +
        "以JSON格式返回：{ \"originalTitle\": \"...\", \"rewrittenVersion\": \"...\", \"changes\": [\"...\"] }";

    private static readonly string ExtractSystemPrompt =
        "你是一个剧本分析专家。从给定的故事内容中提取所有角色、道具、场景和BGM。" +
        "每个资产需要名称和详细描述，适合用于AI生图提示词。" +
        "以JSON格式返回：{ \"actors\": [{ \"name\": \"...\", \"description\": \"...\" }], \"props\": [...], \"scenes\": [...], \"bgms\": [...] }";

    private static readonly string CharacterProfileSystemPrompt =
        "你是一个角色设计师。根据角色描述，生成完整的角色档案，包括外貌特征、性格特点、背景故事和英文生图提示词。" +
        "以JSON格式返回：{ \"name\": \"...\", \"appearance\": \"...\", \"personality\": \"...\", \"background\": \"...\", \"imagePrompt\": \"...\" }";

    private static readonly string SceneProfileSystemPrompt =
        "你是一个场景设计师。根据场景描述，生成详细的场景档案和英文生图提示词。" +
        "以JSON格式返回：{ \"name\": \"...\", \"description\": \"...\", \"imagePrompt\": \"...\" }";

    private static readonly string PropProfileSystemPrompt =
        "你是一个道具设计师。根据道具描述，生成详细的道具档案和英文生图提示词。" +
        "以JSON格式返回：{ \"name\": \"...\", \"description\": \"...\", \"imagePrompt\": \"...\" }";

    private static readonly string SkillProfileSystemPrompt =
        "你是一个技能特效设计师。根据技能描述，生成技能档案和英文生图提示词。" +
        "以JSON格式返回：{ \"name\": \"...\", \"description\": \"...\", \"imagePrompt\": \"...\" }";

    private static readonly string BgmPromptSystemPrompt =
        "你是一个音乐制作人。根据BGM描述，生成详细的音乐风格说明和英文音频生成提示词。" +
        "以JSON格式返回：{ \"name\": \"...\", \"description\": \"...\", \"bgmPrompt\": \"...\" }";

    private static readonly string VideoPromptSystemPrompt =
        "你是一个视频导演。根据动态描述生成视频生成提示词，包括运镜、氛围、画质要求。" +
        "以JSON格式返回：{ \"description\": \"...\", \"videoPrompt\": \"...\", \"negativePrompt\": \"...\", \"parameters\": { \"duration\": 8, \"fps\": 24, \"resolution\": \"1920x1080\" } }";

    static string SampleChapters(string title) => $$"""
        {
          "title": "{{title}}",
          "chapters": [
            {
              "chapterNumber": "第一章",
              "chapterName": "{{title}}之始",
              "content": "在遥远的大陆上，命运之轮开始转动。{{title}}的故事就此展开。\n\n清晨的第一缕阳光穿透云层，照耀在这片古老的土地上。主人公站在山巅，眺望着远方的未知旅程。\n\n心中怀揣着梦想与勇气，踏上了这条充满挑战的道路。"
            },
            {
              "chapterNumber": "第二章",
              "chapterName": "征程的考验",
              "content": "旅途中遇到了志同道合的伙伴。\n\n他们一起穿越了幽暗的森林，跨过了湍急的河流。每一步都充满了未知的挑战。\n\n然而正是这些考验，让彼此之间的羁绊变得更加深厚。"
            },
            {
              "chapterNumber": "第三章",
              "chapterName": "光与暗的对决",
              "content": "最终的决战在古老的遗迹中展开。\n\n面对强大的黑暗势力，主人公与伙伴们齐心协力，发挥出了超乎想象的力量。\n\n光明终将驱散黑暗，正义必将战胜邪恶。这是一个关于勇气、友情与希望的传说。"
            }
          ]
        }
        """;

    static string SampleRewrite(string original) => $$"""
        {
          "originalTitle": "原故事",
          "rewrittenVersion": "【改写版】\n\n{{original}}\n\n---\n\n改写说明：根据用户要求对故事进行了重新创作。新版本在保留核心情节的基础上，增强了情感描写和场景细节，使故事更加生动立体。\n\n主要改动：\n1. 增加了环境氛围的渲染\n2. 深化了角色的内心独白\n3. 优化了对话节奏",
          "changes": ["增强情感描写", "优化叙事节奏", "丰富场景细节"]
        }
        """;

    static string SampleExtract => """
        {
          "actors": [
            { "name": "凯恩", "description": "年轻勇敢的骑士，棕发碧眼，身穿银色铠甲，性格正直坚毅" },
            { "name": "莉娜", "description": "精灵弓箭手，金色长发，尖耳，身穿翠绿斗篷，善用自然魔法" },
            { "name": "莫格莱德", "description": "白发老者法师，手持金色法杖，智慧深邃，精通古代法术" }
          ],
          "props": [
            { "name": "失落之宝", "description": "上古神器，散发着金色光芒的水晶宝石，拥有改变世界的力量" },
            { "name": "银霜剑", "description": "凯恩的佩剑，由北方寒铁铸造，剑身泛着银白色寒光" }
          ],
          "scenes": [
            { "name": "宁静村庄", "description": "被群山环绕的田园村落，清澈溪流穿村而过，屋顶升起袅袅炊烟" },
            { "name": "暗影森林", "description": "幽暗茂密的古老森林，高耸入云的树木遮天蔽日，林间弥漫着薄雾" },
            { "name": "水晶洞窟", "description": "地下深处的水晶宫殿，墙壁镶嵌着各色晶石，散发出梦幻般的荧光" }
          ],
          "bgms": [
            { "name": "冒险启程", "description": "激昂的管弦乐，以小号与弦乐为主，充满希望与力量" },
            { "name": "暗流涌动", "description": "低沉的电子合成音色，伴随着隐约的鼓点，营造紧张悬疑氛围" }
          ]
        }
        """;

    static string SampleProfile(string desc) => $$"""
        {
          "name": "档案角色",
          "appearance": "【外貌特征】\n面容：棱角分明，剑眉星目，鼻梁高挺\n发色：深棕色短发，略带凌乱\n身材：身高约178cm，体型匀称矫健\n服饰：银色锁子甲外罩深蓝披风，腰间佩戴长剑\n特征：右眉梢有一道浅浅的疤痕，目光坚定有力",
          "personality": "【性格特点】\n正直勇敢、重情重义、有时过于理想主义\n内心温柔但外表刚硬\n对弱者有着强烈的保护欲",
          "background": "【背景故事】\n出身于边境小镇的普通家庭，自幼听闻英雄传说，立志成为守护他人的骑士。",
          "imagePrompt": "A heroic knight in silver armor, dramatic lighting, epic fantasy atmosphere, detailed character design, digital art style"
        }
        """;

    static string SampleSceneProfile(string desc) => $$"""
        {
          "name": "场景",
          "description": "【场景描述】\n{{desc}}\n\n【氛围】\n宁静而神秘，空气中弥漫着淡淡的薄雾，光影交错",
          "imagePrompt": "Fantasy landscape, misty atmosphere, dramatic lighting, wide angle view, detailed environment, cinematic composition"
        }
        """;

    static string SamplePropProfile(string desc) => $$"""
        {
          "name": "道具",
          "description": "【道具描述】\n{{desc}}\n\n【细节特征】\n表面流转着若隐若现的符文光芒，触感温润",
          "imagePrompt": "Detailed fantasy item, glowing magical artifact, intricate design, macro photography, dramatic lighting"
        }
        """;

    static string SampleSkillProfile(string desc) => $$"""
        {
          "name": "技能",
          "description": "【技能描述】\n{{desc}}\n\n【效果表现】\n施展时周围空间会产生能量波动，光芒从体内涌出",
          "imagePrompt": "Magic spell casting, energy waves, brilliant light effects, dynamic action pose, fantasy magic style"
        }
        """;

    static string SampleBgm(string desc) => $$"""
        {
          "name": "BGM",
          "description": "【BGM描述】\n{{desc}}\n\n【风格】\n管弦乐与电子音色融合，节奏由舒缓推向高潮",
          "bgmPrompt": "Epic orchestral music with electronic elements, building from calm to powerful climax, cinematic atmosphere, emotional melody"
        }
        """;

    static string SampleVideo(string desc) => $$"""
        {
          "description": "{{desc}}",
          "videoPrompt": "Cinematic footage, {{desc}}, smooth camera movement, high quality, 4K, dramatic lighting",
          "negativePrompt": "blurry, low quality, distorted, ugly, bad anatomy",
          "parameters": { "duration": 8, "fps": 24, "resolution": "1920x1080" }
        }
        """;

    async Task<string> CallLlmAsync(string systemPrompt, string userContent, string fallback, CancellationToken ct)
    {
        var result = await _llm.GenerateAsync(systemPrompt, userContent, ct);
        if (string.IsNullOrEmpty(result))
        {
            _logger.LogWarning("LLM returned empty, using fallback mock data");
            return fallback;
        }
        return result;
    }

    public Task<string> GenerateStoryAsync(string title, string prompt, CancellationToken ct = default) =>
        CallLlmAsync(StorySystemPrompt, $"标题：{title}\n故事主题：{prompt}", SampleChapters(title), ct);

    public Task<string> RewriteStoryAsync(string prompt, string originalStory, CancellationToken ct = default) =>
        CallLlmAsync(RewriteSystemPrompt, $"改写要求：{prompt}\n原始故事：{originalStory}", SampleRewrite(originalStory), ct);

    public Task<string> ExtractAssetsAsync(string story, CancellationToken ct = default) =>
        CallLlmAsync(ExtractSystemPrompt, $"故事内容：{story}", SampleExtract, ct);

    public Task<string> CreateCharacterProfileAsync(string characterDescription, CancellationToken ct = default) =>
        CallLlmAsync(CharacterProfileSystemPrompt, $"角色描述：{characterDescription}", SampleProfile(characterDescription), ct);

    public Task<string> CreateSceneProfileAsync(string sceneDescription, CancellationToken ct = default) =>
        CallLlmAsync(SceneProfileSystemPrompt, $"场景描述：{sceneDescription}", SampleSceneProfile(sceneDescription), ct);

    public Task<string> CreatePropProfileAsync(string propDescription, CancellationToken ct = default) =>
        CallLlmAsync(PropProfileSystemPrompt, $"道具描述：{propDescription}", SamplePropProfile(propDescription), ct);

    public Task<string> CreateSkillProfileAsync(string skillDescription, CancellationToken ct = default) =>
        CallLlmAsync(SkillProfileSystemPrompt, $"技能描述：{skillDescription}", SampleSkillProfile(skillDescription), ct);

    public Task<string> CreateBgmPromptAsync(string bgmDescription, CancellationToken ct = default) =>
        CallLlmAsync(BgmPromptSystemPrompt, $"BGM描述：{bgmDescription}", SampleBgm(bgmDescription), ct);

    public Task<string> CreateVideoPromptAsync(string dynamicDescription, List<string>? referenceImages = null, CancellationToken ct = default) =>
        CallLlmAsync(VideoPromptSystemPrompt, $"动态描述：{dynamicDescription}" + (referenceImages?.Count > 0 ? $"\n参考图：{string.Join(", ", referenceImages)}" : ""), SampleVideo(dynamicDescription), ct);
}