namespace ManjuCraft.Application.Service.Dtos;

public class BulkCreateDto
{
    public long ProjectId { get; set; }
    public List<CreateAssetDto> Assets { get; set; } = new();
}
