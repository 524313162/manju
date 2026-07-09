namespace ManjuCraft.Application.Service.Dtos;

public class ChapterCreateRequestDto
{
    public long StoryId { get; set; }
    public string ChapterName { get; set; } = default!;
    public string Content { get; set; } = default!;
}
