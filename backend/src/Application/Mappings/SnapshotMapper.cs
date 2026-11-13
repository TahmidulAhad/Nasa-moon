using System.Text.Json;
using NasaMoon.Application.Dtos;
using NasaMoon.Domain.Entities;

namespace NasaMoon.Application.Mappings;

/// <summary>
/// Snapshot <-> DTO mapping, plus the JSON helpers that keep every snapshot
/// in exactly the shape the API returns (MissionDto, camelCase) — the same
/// shape as the frontend's static src/data/missions.json.
/// </summary>
public static class SnapshotMapper
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static string SerializeMissions(List<MissionDto> missions)
        => JsonSerializer.Serialize(missions, JsonOptions);

    public static List<MissionDto> DeserializeMissions(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<MissionDto>>(json, JsonOptions) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }

    public static SnapshotDto ToSummaryDto(DataSnapshot s)
    {
        var missions = DeserializeMissions(s.MissionsJson);
        return new SnapshotDto
        {
            Id = s.Id,
            Label = s.Label,
            Source = s.Source,
            CreatedAt = s.CreatedAt,
            MissionCount = missions.Count
        };
    }

    public static SnapshotDetailDto ToDetailDto(DataSnapshot s)
    {
        var summary = ToSummaryDto(s);
        return new SnapshotDetailDto
        {
            Id = summary.Id,
            Label = summary.Label,
            Source = summary.Source,
            CreatedAt = summary.CreatedAt,
            MissionCount = summary.MissionCount,
            Missions = DeserializeMissions(s.MissionsJson)
        };
    }
}
