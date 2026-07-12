using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.Service.Dtos;

public class CreateAssetDto
{
    public long ProjectId { get; set; }
    public string AssetType { get; set; } = "Actor";
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int Order { get; set; }
    public bool Override { get; set; }
}
