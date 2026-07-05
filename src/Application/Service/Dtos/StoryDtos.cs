namespace ManjuCraft.Application.Service.Dtos;

public class ChapterCreateRequestDto
{
    public long StoryId { get; set; }
    public string ChapterName { get; set; } = default!;
    public string Content { get; set; } = default!;
}

public class ChapterEditRequestDto
{
    public long Id { get; set; }
    public string ChapterName { get; set; } = default!;
    public string Content { get; set; } = default!;
}

public class ChapterDeleteRequestDto
{
    public long Id { get; set; }
}

public class ImportScriptDto
{
    public string ScriptJson { get; set; } = "";
}
