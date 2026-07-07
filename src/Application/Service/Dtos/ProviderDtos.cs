namespace ManjuCraft.Application.Service.Dtos;

public class ProviderRequestDto
{
    public string Name { get; set; } = default!;
    public string Capability { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string ApiUrl { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string Model { get; set; } = default!;
}

public class DeleteRequestDto
{
    public long Id { get; set; }
}
