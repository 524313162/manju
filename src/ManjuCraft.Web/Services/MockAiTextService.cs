using ManjuCraft.Application.Service;

namespace ManjuCraft.Web.Services;

public class MockAiTextService : IAiTextService
{
    public async Task<string> GenerateStoryAsync(string title, string prompt, CancellationToken ct = default)
    {
        await Task.Delay(800, ct);
        return $@"{{
  ""title"": ""{title}"",
  ""chapters"": [
    {{
      ""chapterNumber"": ""第一章"",
      ""chapterName"": ""{title}之始"",
      ""content"": ""在遥远的大陆上，命运之轮开始转动。{title}的故事就此展开。\n\n清晨的第一缕阳光穿透云层，照耀在这片古老的土地上。主人公站在山巅，眺望着远方的未知旅程。\n\n心中怀揣着梦想与勇气，踏上了这条充满挑战的道路。""
    }},
    {{
      ""chapterNumber"": ""第二章"",
      ""chapterName"": ""征程的考验"",
      ""content"": ""旅途中遇到了志同道合的伙伴。\n\n他们一起穿越了幽暗的森林，跨过了湍急的河流。每一步都充满了未知的挑战。\n\n然而正是这些考验，让彼此之间的羁绊变得更加深厚。""
    }},
    {{
      ""chapterNumber"": ""第三章"",
      ""chapterName"": ""光与暗的对决"",
      ""content"": ""最终的决战在古老的遗迹中展开。\n\n面对强大的黑暗势力，主人公与伙伴们齐心协力，发挥出了超乎想象的力量。\n\n光明终将驱散黑暗，正义必将战胜邪恶。这是一个关于勇气、友情与希望的传说。""
    }}
  ]
}}";
    }

    public async Task<string> RewriteStoryAsync(string prompt, string originalStory, CancellationToken ct = default)
    {
        await Task.Delay(600, ct);
        return $@"{{
  ""originalTitle"": ""原故事"",
  ""rewrittenVersion"": ""【改写版】\n\n{originalStory}\n\n---\n\n改写说明：根据「{prompt}」的要求，对故事进行了重新创作。新版本在保留核心情节的基础上，增强了情感描写和场景细节，使故事更加生动立体。\n\n主要改动：\n1. 增加了环境氛围的渲染\n2. 深化了角色的内心独白\n3. 优化了对话节奏\n4. 强化了冲突张力"",
  ""changes"": [""增强情感描写"", ""优化叙事节奏"", ""丰富场景细节""]
}}";
    }

    public async Task<string> ExtractAssetsAsync(string story, CancellationToken ct = default)
    {
        await Task.Delay(700, ct);
        return @"{
  ""actors"": [
    { ""name"": ""凯恩"", ""description"": ""年轻勇敢的骑士，棕发碧眼，身穿银色铠甲，性格正直坚毅"" },
    { ""name"": ""莉娜"", ""description"": ""精灵弓箭手，金色长发，尖耳，身穿翠绿斗篷，善用自然魔法"" },
    { ""name"": ""莫格莱德"", ""description"": ""白发老者法师，手持金色法杖，智慧深邃，精通古代法术"" }
  ],
  ""props"": [
    { ""name"": ""失落之宝"", ""description"": ""上古神器，散发着金色光芒的水晶宝石，拥有改变世界的力量"" },
    { ""name"": ""银霜剑"", ""description"": ""凯恩的佩剑，由北方寒铁铸造，剑身泛着银白色寒光"" }
  ],
  ""scenes"": [
    { ""name"": ""宁静村庄"", ""description"": ""被群山环绕的田园村落，清澈溪流穿村而过，屋顶升起袅袅炊烟"" },
    { ""name"": ""暗影森林"", ""description"": ""幽暗茂密的古老森林，高耸入云的树木遮天蔽日，林间弥漫着薄雾"" },
    { ""name"": ""水晶洞窟"", ""description"": ""地下深处的水晶宫殿，墙壁镶嵌着各色晶石，散发出梦幻般的荧光"" }
  ],
  ""bgms"": [
    { ""name"": ""冒险启程"", ""description"": ""激昂的管弦乐，以小号与弦乐为主，充满希望与力量"" },
    { ""name"": ""暗流涌动"", ""description"": ""低沉的电子合成音色，伴随着隐约的鼓点，营造紧张悬疑氛围"" }
  ]
}";
    }

    public async Task<string> CreateCharacterProfileAsync(string characterDescription, CancellationToken ct = default)
    {
        await Task.Delay(500, ct);
        return $@"{{
  ""name"": ""基于「{characterDescription}」生成"",
  ""appearance"": ""【外貌特征】\n面容：棱角分明，剑眉星目，鼻梁高挺\n发色：深棕色短发，略带凌乱\n身材：身高约178cm，体型匀称矫健\n服饰：银色锁子甲外罩深蓝披风，腰间佩戴长剑\n特征：右眉梢有一道浅浅的疤痕，目光坚定有力"",
  ""personality"": ""【性格特点】\n正直勇敢、重情重义、有时过于理想主义\n内心温柔但外表刚硬\n对弱者有着强烈的保护欲"",
  ""background"": ""【背景故事】\n出身于边境小镇的普通家庭，自幼听闻英雄传说，立志成为守护他人的骑士。\n在经历了一系列冒险后逐渐成长为独当一面的战士。"",
  ""imagePrompt"": ""A heroic knight in silver armor with brown hair, standing in a fantasy landscape, dramatic lighting, epic atmosphere, detailed character design, digital art style""
}}";
    }

    public async Task<string> CreateSceneProfileAsync(string sceneDescription, CancellationToken ct = default)
    {
        await Task.Delay(500, ct);
        return $@"{{
  ""name"": ""基于「{sceneDescription}」生成"",
  ""description"": ""【场景描述】\n{sceneDescription}\n\n【氛围】\n宁静而神秘，空气中弥漫着淡淡的薄雾\n光影交错，营造出梦幻般的视觉效果"",
  ""imagePrompt"": ""Fantasy landscape, misty atmosphere, dramatic lighting, wide angle view, detailed environment, cinematic composition""
}}";
    }

    public async Task<string> CreatePropProfileAsync(string propDescription, CancellationToken ct = default)
    {
        await Task.Delay(500, ct);
        return $@"{{
  ""name"": ""基于「{propDescription}」生成"",
  ""description"": ""【道具描述】\n{propDescription}\n\n【细节特征】\n表面流转着若隐若现的符文光芒\n触感温润，散发出微弱的力量波动"",
  ""imagePrompt"": ""Detailed fantasy item, glowing magical artifact, intricate design, macro photography style, dramatic lighting, dark background""
}}";
    }

    public async Task<string> CreateSkillProfileAsync(string skillDescription, CancellationToken ct = default)
    {
        await Task.Delay(500, ct);
        return $@"{{
  ""name"": ""基于「{skillDescription}」生成"",
  ""description"": ""【技能描述】\n{skillDescription}\n\n【效果表现】\n施展时周围空间会产生能量波动\n光芒从施法者体内涌出，形成华丽的视觉效果"",
  ""imagePrompt"": ""Magic spell casting, energy waves, brilliant light effects, dynamic action pose, fantasy magic style""
}}";
    }

    public async Task<string> CreateBgmPromptAsync(string bgmDescription, CancellationToken ct = default)
    {
        await Task.Delay(500, ct);
        return $@"{{
  ""name"": ""基于「{bgmDescription}」生成"",
  ""description"": ""【BGM描述】\n{bgmDescription}\n\n【风格】\n管弦乐与电子音色融合\n节奏：中速，由舒缓逐渐推向高潮\n情绪：从平静到激昂，带有叙事感"",
  ""bgmPrompt"": ""Epic orchestral music with electronic elements, building from calm to powerful climax, cinematic atmosphere, emotional melody""
}}";
    }

    public async Task<string> CreateVideoPromptAsync(string dynamicDescription, List<string>? referenceImages = null, CancellationToken ct = default)
    {
        await Task.Delay(600, ct);
        return $@"{{
  ""description"": ""{dynamicDescription}"",
  ""videoPrompt"": ""Cinematic footage, {dynamicDescription}, smooth camera movement, high quality, 4K, dramatic lighting, atmospheric"",
  ""negativePrompt"": ""blurry, low quality, distorted, ugly, bad anatomy"",
  ""parameters"": {{
    ""duration"": 8,
    ""fps"": 24,
    ""resolution"": ""1920x1080""
  }}
}}";
    }
}