namespace NasaMoon.Domain.Entities;

/// <summary>A verifiable source (NASA / USGS page) backing a mission's facts.</summary>
public class Source
{
    public int Id { get; set; }
    public string MissionId { get; set; } = string.Empty;
    public Mission? Mission { get; set; }

    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateOnly? Accessed { get; set; }
}
