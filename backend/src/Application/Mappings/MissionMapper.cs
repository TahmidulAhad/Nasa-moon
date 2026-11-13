using NasaMoon.Application.Dtos;
using NasaMoon.Domain.Entities;

namespace NasaMoon.Application.Mappings;

public static class MissionMapper
{
    public static MissionDto ToDto(Mission m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        TaskOrder = m.TaskOrder,
        Provider = m.Provider,
        Lander = m.Lander,
        Status = m.Status,
        LaunchDate = m.LaunchDate?.ToString("yyyy-MM-dd"),
        LaunchDatePrecision = m.LaunchDatePrecision,
        LandingDate = m.LandingDate?.ToString("yyyy-MM-dd"),
        LandingDatePrecision = m.LandingDatePrecision,
        LandingSite = m.LandingSiteName is null && m.Latitude is null && m.Longitude is null && m.Region is null
            ? null
            : new LandingSiteDto
            {
                Name = m.LandingSiteName,
                Latitude = m.Latitude,
                Longitude = m.Longitude,
                Region = m.Region,
                CoordinatesApproximate = m.CoordinatesApproximate
            },
        PayloadCountReported = m.PayloadCountReported,
        Objective = m.Objective,
        OutcomeSummary = m.OutcomeSummary,
        Notes = m.Notes,
        LastVerified = m.LastVerified?.ToString("yyyy-MM-dd"),
        Payloads = m.Payloads.Select(p => new PayloadDto
        {
            Id = p.Id,
            Name = p.Name,
            Organization = p.Organization,
            Purpose = p.Purpose,
            Type = p.Type
        }).ToList(),
        Sources = m.Sources.Select(s => new SourceDto
        {
            Id = s.Id,
            Label = s.Label,
            Url = s.Url,
            Accessed = s.Accessed?.ToString("yyyy-MM-dd")
        }).ToList()
    };

    public static Mission ToEntity(CreateMissionDto dto) => new()
    {
        Id = dto.Id.Trim().ToLowerInvariant(),
        Name = dto.Name,
        TaskOrder = dto.TaskOrder,
        Provider = dto.Provider,
        Lander = dto.Lander,
        Status = dto.Status,
        LaunchDate = ParseDate(dto.LaunchDate),
        LaunchDatePrecision = dto.LaunchDatePrecision,
        LandingDate = ParseDate(dto.LandingDate),
        LandingDatePrecision = dto.LandingDatePrecision,
        LandingSiteName = dto.LandingSite?.Name,
        Latitude = dto.LandingSite?.Latitude,
        Longitude = dto.LandingSite?.Longitude,
        Region = dto.LandingSite?.Region,
        CoordinatesApproximate = dto.LandingSite?.CoordinatesApproximate ?? false,
        PayloadCountReported = dto.PayloadCountReported,
        Objective = dto.Objective,
        OutcomeSummary = dto.OutcomeSummary,
        Notes = dto.Notes
    };

    public static void ApplyUpdate(Mission m, UpdateMissionDto dto)
    {
        m.Name = dto.Name;
        m.TaskOrder = dto.TaskOrder;
        m.Provider = dto.Provider;
        m.Lander = dto.Lander;
        m.Status = dto.Status;
        m.LaunchDate = ParseDate(dto.LaunchDate);
        m.LaunchDatePrecision = dto.LaunchDatePrecision;
        m.LandingDate = ParseDate(dto.LandingDate);
        m.LandingDatePrecision = dto.LandingDatePrecision;
        m.LandingSiteName = dto.LandingSite?.Name;
        m.Latitude = dto.LandingSite?.Latitude;
        m.Longitude = dto.LandingSite?.Longitude;
        m.Region = dto.LandingSite?.Region;
        m.CoordinatesApproximate = dto.LandingSite?.CoordinatesApproximate ?? false;
        m.PayloadCountReported = dto.PayloadCountReported;
        m.Objective = dto.Objective;
        m.OutcomeSummary = dto.OutcomeSummary;
        m.Notes = dto.Notes;
        m.UpdatedAt = DateTime.UtcNow;
    }

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, out var d) ? d : null;
}
