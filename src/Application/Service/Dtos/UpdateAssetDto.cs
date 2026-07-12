namespace ManjuCraft.Application.Service.Dtos;

public class UpdateAssetDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? AssetType { get; set; }
    public string? ParentId { get; set; }
    public int? Order { get; set; }
}
