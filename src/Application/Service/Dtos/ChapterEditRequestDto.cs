namespace ManjuCraft.Application.Service.Dtos;

public class ChapterEditRequestDto
{
    public long Id { get; set; }
    public string ChapterName { get; set; } = default!;
    public string Content { get; set; } = default!;
}
