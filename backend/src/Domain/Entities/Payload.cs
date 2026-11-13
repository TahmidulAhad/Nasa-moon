namespace NasaMoon.Domain.Entities;

/// <summary>A science payload carried by a CLPS mission.</summary>
public class Payload
{
    public int Id { get; set; }
    public string MissionId { get; set; } = string.Empty;
    public Mission? Mission { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string? Purpose { get; set; }
    public string? Type { get; set; }
}
