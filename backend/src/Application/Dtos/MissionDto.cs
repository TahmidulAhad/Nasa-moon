namespace NasaMoon.Application.Dtos;

public class LandingSiteDto
{
    public string? Name { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Region { get; set; }
    public bool CoordinatesApproximate { get; set; }
}

public class PayloadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string? Purpose { get; set; }
    public string? Type { get; set; }
}

public class SourceDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Accessed { get; set; }
}

public class MissionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaskOrder { get; set; }
    public string? Provider { get; set; }
    public string? Lander { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? LaunchDate { get; set; }
    public string? LaunchDatePrecision { get; set; }
    public string? LandingDate { get; set; }
    public string? LandingDatePrecision { get; set; }
    public LandingSiteDto? LandingSite { get; set; }
    public int? PayloadCountReported { get; set; }
    public string? Objective { get; set; }
    public string? OutcomeSummary { get; set; }
    public string? Notes { get; set; }
    public string? LastVerified { get; set; }
    public List<PayloadDto> Payloads { get; set; } = new();
    public List<SourceDto> Sources { get; set; } = new();
}

/// <summary>Query filters for GET /api/missions — mirrors the frontend FilterBar.</summary>
public class MissionFilter
{
    /// <summary>Free-text search across name, lander, provider, landing site and payloads.</summary>
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Provider { get; set; }
    public int? LaunchYear { get; set; }
    public string? Region { get; set; }
}

public class CreateMissionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaskOrder { get; set; }
    public string? Provider { get; set; }
    public string? Lander { get; set; }
    public string Status { get; set; } = "upcoming";
    public string? LaunchDate { get; set; }
    public string? LaunchDatePrecision { get; set; }
    public string? LandingDate { get; set; }
    public string? LandingDatePrecision { get; set; }
    public LandingSiteDto? LandingSite { get; set; }
    public int? PayloadCountReported { get; set; }
    public string? Objective { get; set; }
    public string? OutcomeSummary { get; set; }
    public string? Notes { get; set; }
}

public class UpdateMissionDto : CreateMissionDto
{
}
