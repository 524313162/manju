namespace ManjuCraft.Application.Service.Dtos;

public class TemplateRequestDto
{
    public string Name { get; set; } = default!;
    public string TemplateType { get; set; } = default!;
    public string Content { get; set; } = default!;
    public bool IsDefault { get; set; }
}
