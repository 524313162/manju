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

        // Seed test data if database is empty
        if (!db.Projects.Any())
        {
            await SeedTestDataAsync(db);
        }

        var hasChanges = db.ChangeTracker.Entries().Any(e => e.State == EntityState.Added);
        if (hasChanges)
        {
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedTestDataAsync(ProjectDbContext db)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Create test project
        var project = new Project
        {
            Name = "测试漫剧项目",
            CreatedTime = now,
            UpdatedTime = now
        };
        await db.Projects.AddAsync(project);
        await db.SaveChangesAsync();

        // Create story
        var story = new Story
        {
            ProjectId = project.Id,
            Title = "盛唐风云",
            Summary = "历史系研究生李轩意外穿越盛唐，成为安西都护之子、新晋驸马，誓要守护太平公主。",
            CreatedTime = now,
            UpdatedTime = now
        };
        await db.Stories.AddAsync(story);
        await db.SaveChangesAsync();

        // Create chapters
        var chapters = new List<StoryChapter>
        {
            new StoryChapter
            {
                StoryId = story.Id,
                ChapterNumber = 1,
                ChapterName = "初醒公主府",
                Content = @"清晨，李轩从雕花床上惊醒，脑中还残留着现代图书馆里的轰鸣声。他本是历史系研究生，却在一场意外中魂穿盛唐，成了安西都护之子、新晋驸马。太平公主端着一碗醒酒汤推门而入，眉目间既有皇室贵气又带着少女的俏皮。李轩愣愣地看着她，想起了史书中关于这位公主的记载——权倾朝野、最终死于政变。他暗暗发誓，这一世定要护她周全。

太平见他发呆，笑着将汤碗递到他唇边，李轩接过时指尖相触，两人都红了脸。随后两人在公主府花园中游玩赏花，李轩用现代知识解释星象与农时，引得太平公主惊叹连连。情愫暗生间，两人结为真正夫妻，许下白头偕老的誓言。",
                SortOrder = 0,
                CreatedTime = now,
                UpdatedTime = now
            },
            new StoryChapter
            {
                StoryId = story.Id,
                ChapterNumber = 2,
                ChapterName = "朝堂风云起",
                Content = @"翌日早朝，李轩随父安西都护入宫觐见。太平公主因功封邑扩大，引来朝中大臣忌惮。宰相李林甫借机进谗，欲以""谋逆""罪名除之。李轩在殿上力争，引经据典，反倒让李林甫哑口无言。圣上龙颜大悦，赐李轩""护国将军""封号，命其统领禁军护卫太平公主府安全。

暗流涌动中，李轩察觉有人欲对公主不利，便在府中布下天罗地网。夜半，刺客潜入，被李轩早有准备的禁军一网打尽。查明主使竟是李林甫心腹，李轩面圣请旨彻查，朝堂震动。",
                SortOrder = 1,
                CreatedTime = now,
                UpdatedTime = now
            },
            new StoryChapter
            {
                StoryId = story.Id,
                ChapterNumber = 3,
                ChapterName = "边关烽火急",
                Content = @"安西都护府传来急报，吐蕃大军压境，安西四镇告急。李轩请缨出征，太平公主强忍泪水为他整甲披挂，赠以护身玉佩。李轩率三千精骑星夜驰援，沿途收编溃兵，以寡敌众，在大非川设伏歼敌五千。

大胜班师，圣上大宴群臣，封李轩为""定远大将军"", 赐紫金冠、黄金甲。太平公主在宫中遥望凯旋方向，眼中满是骄傲与担忧。李林甫见势不妙，转而拉拢太子李亨，欲以东宫制衡太平。",
                SortOrder = 2,
                CreatedTime = now,
                UpdatedTime = now
            }
        };

        await db.StoryChapters.AddRangeAsync(chapters);
        await db.SaveChangesAsync();

        // Create episodes for each chapter
        var episodes = new List<Episode>();
        for (int i = 0; i < chapters.Count; i++)
        {
            episodes.Add(new Episode
            {
                ProjectId = project.Id,
                StoryChapterId = chapters[i].Id,
                Name = chapters[i].ChapterName,
                Duration = 0,
                Order = i,
                CreatedTime = now,
                UpdatedTime = now
            });
        }
        await db.Episodes.AddRangeAsync(episodes);
        await db.SaveChangesAsync();

        // Create assets
        var assets = new List<Asset>
        {
            new Asset { ProjectId = project.Id, AssetType = AssetTypeEnum.Actor, Name = "李轩(男一号)", Description = "李轩（男一号），历史系研究生穿越而来，安西都护之子、新晋驸马。身高185cm，黑发，身着玄色锦袍，腰佩玉饰，眼神深邃坚毅，标志性动作：握拳、抚剑。", Order = 0, CreatedTime = now, UpdatedTime = now },
            new Asset { ProjectId = project.Id, AssetType = AssetTypeEnum.Actor, Name = "太平公主(女一号)", Description = "太平公主（女一号），唐睿宗之女，武则天孙女。眉目如画，兼具皇室贵气与少女俏皮。身着华贵宫装，发簪珠翠流苏，标志性动作：端汤碗、理衣襟。", Order = 1, CreatedTime = now, UpdatedTime = now },
            new Asset { ProjectId = project.Id, AssetType = AssetTypeEnum.Scene, Name = "公主府", Description = "皇家府邸，内有雕花大床、花园、书房、偏殿。整体装饰奢华典雅，气氛从温馨逐渐转为紧张。作为主要起居场景出现在1、4、5章。", Order = 0, CreatedTime = now, UpdatedTime = now },
            new Asset { ProjectId = project.Id, AssetType = AssetTypeEnum.Scene, Name = "大殿", Description = "大唐太极殿，朱红宫墙，琉璃瓦顶，金銮宝座巍峨。文武百官分列两侧，气氛肃杀。朝堂博弈核心场景。", Order = 1, CreatedTime = now, UpdatedTime = now },
            new Asset { ProjectId = project.Id, AssetType = AssetTypeEnum.Scene, Name = "大非川", Description = "川藏高原险要隘口，两侧绝壁如削，河水奔腾。李轩率三千精骑在此设伏歼敌五千，以寡敌众的经典战场。", Order = 2, CreatedTime = now, UpdatedTime = now },
            new Asset { ProjectId = project.Id, AssetType = AssetTypeEnum.Prop, Name = "醒酒汤", Description = "太平公主亲手熬制的醒酒汤，青瓷碗盛装，热气腾腾，象征关怀与情谊。", Order = 0, CreatedTime = now, UpdatedTime = now },
            new Asset { ProjectId = project.Id, AssetType = AssetTypeEnum.Prop, Name = "护身玉佩", Description = "太平公主赠予李轩的护身玉佩，温润如玉，刻有护身符咒，寓意平安归来。", Order = 1, CreatedTime = now, UpdatedTime = now },
            new Asset { ProjectId = project.Id, AssetType = AssetTypeEnum.Prop, Name = "紫金冠", Description = "圣上赐予李轩的紫金冠，金线编织，镶嵌珍珠翡翠，象征定远大将军的崇高荣誉。", Order = 2, CreatedTime = now, UpdatedTime = now }
        };
        await db.Assets.AddRangeAsync(assets);
        await db.SaveChangesAsync();

        // Create shots for Chapter 1 (Episode 0)
        var episode1 = episodes[0];
        var shots = new List<Shot>
        {
            new Shot
            {
                EpisodeId = episode1.Id,
                ShotNumber = "SH001",
                Description = "清晨公主府",
                ShotSize = "全景",
                CameraMovement = "固定",
                Duration = 8,
                Order = 0,
                CreatedTime = now,
                UpdatedTime = now
            },
            new Shot
            {
                EpisodeId = episode1.Id,
                ShotNumber = "SH002",
                Description = "李轩和太平公主游园赏花",
                ShotSize = "全景",
                CameraMovement = "平移",
                Duration = 8,
                Order = 1,
                CreatedTime = now,
                UpdatedTime = now
            }
        };
        await db.Shots.AddRangeAsync(shots);
        await db.SaveChangesAsync();

        // Create ShotAssets
        var shotAssets = new List<ShotAsset>
        {
            new ShotAsset { ShotId = shots[0].Id, AssetId = assets.First(a => a.Name == "公主府").Id, Role = "场景", Order = 0, CreatedTime = now, UpdatedTime = now },
            new ShotAsset { ShotId = shots[0].Id, AssetId = assets.First(a => a.Name == "李轩(男一号)").Id, Role = "主角", Order = 1, CreatedTime = now, UpdatedTime = now },
            new ShotAsset { ShotId = shots[0].Id, AssetId = assets.First(a => a.Name == "太平公主(女一号)").Id, Role = "主角", Order = 2, CreatedTime = now, UpdatedTime = now },
            new ShotAsset { ShotId = shots[0].Id, AssetId = assets.First(a => a.Name == "醒酒汤").Id, Role = "道具", Order = 3, CreatedTime = now, UpdatedTime = now },
            new ShotAsset { ShotId = shots[1].Id, AssetId = assets.First(a => a.Name == "公主府").Id, Role = "场景", Order = 0, CreatedTime = now, UpdatedTime = now },
            new ShotAsset { ShotId = shots[1].Id, AssetId = assets.First(a => a.Name == "李轩(男一号)").Id, Role = "主角", Order = 1, CreatedTime = now, UpdatedTime = now },
            new ShotAsset { ShotId = shots[1].Id, AssetId = assets.First(a => a.Name == "太平公主(女一号)").Id, Role = "主角", Order = 2, CreatedTime = now, UpdatedTime = now }
        };
        await db.ShotAssets.AddRangeAsync(shotAssets);
        await db.SaveChangesAsync();

        // Create ShotFrames for Shot 1
        var frames1 = new List<ShotFrame>
        {
            new ShotFrame { ShotId = shots[0].Id, ProjectId = project.Id, ShotNumber = "SH001", FrameType = "First", Description = "首帧：李轩从雕花床上惊醒，脑中还残留着现代图书馆里的轰鸣声。镜头语言：全景拍摄，展示公主府的奢华典雅，李轩的睡姿和表情。", StartTime = 0f, Duration = 1.5f, Order = 0, CreatedTime = now, UpdatedTime = now },
            new ShotFrame { ShotId = shots[0].Id, ProjectId = project.Id, ShotNumber = "SH001", FrameType = "Middle", Description = "中间帧：太平公主端着一碗醒酒汤推门而入，眉目间既有皇室贵气又带着少女的俏皮。镜头语言：中景拍摄，展示太平公主的美丽和李轩的惊讶。", StartTime = 1.5f, Duration = 1.5f, Order = 1, CreatedTime = now, UpdatedTime = now },
            new ShotFrame { ShotId = shots[0].Id, ProjectId = project.Id, ShotNumber = "SH001", FrameType = "Middle", Description = "中间帧：李轩愣愣地看着太平公主，想起了史书中关于这位公主的记载——权倾朝野、最终死于政变。他暗暗发誓，这一世定要护她周全。镜头语言：近景拍摄，展示李轩的表情和内心世界。", StartTime = 3.0f, Duration = 1.5f, Order = 2, CreatedTime = now, UpdatedTime = now },
            new ShotFrame { ShotId = shots[0].Id, ProjectId = project.Id, ShotNumber = "SH001", FrameType = "Middle", Description = "中间帧：太平见他发呆，笑着将汤碗递到他唇边，李轩接过时指尖相触，两人都红了脸。镜头语言：特写拍摄，展示两人之间的亲密和羞涩。", StartTime = 4.5f, Duration = 1.5f, Order = 3, CreatedTime = now, UpdatedTime = now },
            new ShotFrame { ShotId = shots[0].Id, ProjectId = project.Id, ShotNumber = "SH001", FrameType = "Last", Description = "末帧：李轩和太平公主的对话和互动，镜头语言：中景拍摄，展示两人之间的关系和情感。", StartTime = 6.0f, Duration = 1.5f, Order = 4, CreatedTime = now, UpdatedTime = now }
        };
        await db.ShotFrames.AddRangeAsync(frames1);
        await db.SaveChangesAsync();

        // Create ShotFrames for Shot 2
        var frames2 = new List<ShotFrame>
        {
            new ShotFrame { ShotId = shots[1].Id, ProjectId = project.Id, ShotNumber = "SH002", FrameType = "First", Description = "首帧：李轩和太平公主在公主府的花园中游玩，镜头语言：全景拍摄，展示公主府的美丽和两人之间的关系。", StartTime = 0f, Duration = 1.5f, Order = 0, CreatedTime = now, UpdatedTime = now },
            new ShotFrame { ShotId = shots[1].Id, ProjectId = project.Id, ShotNumber = "SH002", FrameType = "Middle", Description = "中间帧：李轩和太平公主谈古论今，用现代知识解释星象与农时，引得太平公主惊叹连连。镜头语言：中景拍摄，展示两人之间的互动和知识交流。", StartTime = 1.5f, Duration = 1.5f, Order = 1, CreatedTime = now, UpdatedTime = now },
            new ShotFrame { ShotId = shots[1].Id, ProjectId = project.Id, ShotNumber = "SH002", FrameType = "Middle", Description = "中间帧：李轩和太平公主的情愫暗生，镜头语言：近景拍摄，展示两人之间的感情和关系。", StartTime = 3.0f, Duration = 1.5f, Order = 2, CreatedTime = now, UpdatedTime = now },
            new ShotFrame { ShotId = shots[1].Id, ProjectId = project.Id, ShotNumber = "SH002", FrameType = "Middle", Description = "中间帧：李轩和太平公主结为真正夫妻，镜头语言：特写拍摄，展示两人之间的爱情和幸福。", StartTime = 4.5f, Duration = 1.5f, Order = 3, CreatedTime = now, UpdatedTime = now },
            new ShotFrame { ShotId = shots[1].Id, ProjectId = project.Id, ShotNumber = "SH002", FrameType = "Last", Description = "末帧：李轩和太平公主的幸福生活，镜头语言：全景拍摄，展示公主府的美丽和两人之间的关系。", StartTime = 6.0f, Duration = 1.5f, Order = 4, CreatedTime = now, UpdatedTime = now }
        };
        await db.ShotFrames.AddRangeAsync(frames2);
        await db.SaveChangesAsync();
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

            // ═══ NVIDIA NIM (API Catalog) ═══
            new() { Name = "NVIDIA NIM - Nemotron 3 Ultra", Capability = AiCapability.TextToText, Type = ProviderType.LLM,      ApiUrl = "https://integrate.api.nvidia.com/v1",  ApiKey = "", Model = "nvidia/nemotron-3-ultra",   CreatedTime = now, UpdatedTime = now },
            new() { Name = "NVIDIA NIM - Nemotron 4 340B",  Capability = AiCapability.TextToText, Type = ProviderType.LLM,      ApiUrl = "https://integrate.api.nvidia.com/v1",  ApiKey = "", Model = "nvidia/nemotron-4-340b-instruct", CreatedTime = now, UpdatedTime = now },
            new() { Name = "NVIDIA NIM - Llama 3.1 70B",    Capability = AiCapability.TextToText, Type = ProviderType.LLM,      ApiUrl = "https://integrate.api.nvidia.com/v1",  ApiKey = "", Model = "meta/llama-3.1-70b-instruct",  CreatedTime = now, UpdatedTime = now },
            new() { Name = "NVIDIA NIM - Llama 3.1 405B",   Capability = AiCapability.TextToText, Type = ProviderType.LLM,      ApiUrl = "https://integrate.api.nvidia.com/v1",  ApiKey = "", Model = "meta/llama-3.1-405b-instruct", CreatedTime = now, UpdatedTime = now },
            new() { Name = "NVIDIA NIM - Cosmos 1.0",       Capability = AiCapability.TextToVideo, Type = ProviderType.LLM,      ApiUrl = "https://integrate.api.nvidia.com/v1",  ApiKey = "", Model = "nvidia/cosmos-1.0-diffusion", CreatedTime = now, UpdatedTime = now },
            new() { Name = "NVIDIA NIM - Stable Diffusion XL", Capability = AiCapability.TextToImage, Type = ProviderType.LLM,    ApiUrl = "https://integrate.api.nvidia.com/v1",  ApiKey = "", Model = "stabilityai/sdxl", CreatedTime = now, UpdatedTime = now },

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
