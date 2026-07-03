using Microsoft.Extensions.Logging;
using ManjuCraft.Application.LLM;

namespace ManjuCraft.Web.LLM;

public class MockLLMClient : ILLMClient
{
    private readonly ILogger<MockLLMClient> _logger;

    public MockLLMClient(ILogger<MockLLMClient> logger) => _logger = logger;

    public Task<string> GenerateAsync(string systemPrompt, string userContent, CancellationToken ct = default)
    {
        _logger.LogInformation("[MockLLM] system={SysLen}chars user={UserLen}chars", systemPrompt.Length, userContent.Length);

        if (systemPrompt.Contains("剧本创作")) return Task.FromResult(MockStory);
        if (systemPrompt.Contains("剧本改写")) return Task.FromResult(MockRewrite);
        if (systemPrompt.Contains("提取")) return Task.FromResult(MockExtract);
        if (systemPrompt.Contains("角色设计")) return Task.FromResult(MockProfile);
        if (systemPrompt.Contains("场景设计")) return Task.FromResult(MockScene);
        if (systemPrompt.Contains("道具设计")) return Task.FromResult(MockProp);
        if (systemPrompt.Contains("技能特效")) return Task.FromResult(MockSkill);
        if (systemPrompt.Contains("音乐制作")) return Task.FromResult(MockBgm);
        if (systemPrompt.Contains("视频导演")) return Task.FromResult(MockVideo);

        return Task.FromResult("{\"result\": \"ok\"}");
    }

    static string MockStory => """
        {
          "title": "勇者传说",
          "chapters": [
            {"chapterNumber":"第一章","chapterName":"命运的召唤","content":"在遥远的大陆上，勇者踏上了冒险的旅程。\n\n清晨的第一缕阳光穿透云层，主人公站在山巅眺望远方。\n\n心中怀揣着梦想与勇气，踏上了这条充满挑战的道路。"},
            {"chapterNumber":"第二章","chapterName":"征程的考验","content":"旅途中遇到了志同道合的伙伴。\n\n他们一起穿越了幽暗的森林，跨过了湍急的河流。\n\n正是这些考验让彼此的羁绊更加深厚。"},
            {"chapterNumber":"第三章","chapterName":"光与暗的对决","content":"最终的决战在古老的遗迹中展开。\n\n面对强大的黑暗势力，主人公与伙伴们齐心协力。\n\n光明终将驱散黑暗，正义必将战胜邪恶。"}
          ]
        }
        """;

    static string MockRewrite => """
        {"originalTitle":"原故事","rewrittenVersion":"【改写版】增强了情感描写和场景细节。","changes":["增强情感描写","优化叙事节奏","丰富场景细节"]}
        """;

    static string MockExtract => """
        {
          "actors":[{"name":"凯恩","description":"年轻勇敢的骑士，棕发碧眼，身穿银色铠甲"},{"name":"莉娜","description":"精灵弓箭手，金色长发，身穿翠绿斗篷"}],
          "props":[{"name":"失落之宝","description":"上古神器，散发金色光芒的水晶宝石"},{"name":"银霜剑","description":"北方寒铁铸造的佩剑"}],
          "scenes":[{"name":"宁静村庄","description":"被群山环绕的田园村落"},{"name":"暗影森林","description":"幽暗茂密的古老森林"},{"name":"水晶洞窟","description":"地下深处的水晶宫殿"}],
          "bgms":[{"name":"冒险启程","description":"激昂的管弦乐"},{"name":"暗流涌动","description":"低沉的电子合成音色"}]
        }
        """;

    static string MockProfile => """
        {"name":"英雄","appearance":"棱角分明，剑眉星目，深棕色短发，银色铠甲","personality":"正直勇敢、重情重义","background":"出身于边境小镇，立志成为守护他人的骑士","imagePrompt":"A heroic knight in silver armor, dramatic lighting, epic fantasy"}
        """;

    static string MockScene => """
        {"name":"场景","description":"宁静而神秘，薄雾弥漫","imagePrompt":"Fantasy landscape, misty atmosphere, dramatic lighting, cinematic"}
        """;

    static string MockProp => """
        {"name":"道具","description":"表面流转着符文光芒","imagePrompt":"Glowing magical artifact, intricate design, macro photography"}
        """;

    static string MockSkill => """
        {"name":"技能","description":"施展时能量从体内涌出","imagePrompt":"Magic spell casting, energy waves, brilliant light effects"}
        """;

    static string MockBgm => """
        {"name":"BGM","description":"管弦乐与电子音色融合","bgmPrompt":"Epic orchestral music with electronic elements, cinematic"}
        """;

    static string MockVideo => """
        {"description":"动态场景","videoPrompt":"Cinematic footage, smooth camera movement, 4K","negativePrompt":"blurry, low quality","parameters":{"duration":8,"fps":24,"resolution":"1920x1080"}}
        """;
}