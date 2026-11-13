namespace NasaMoon.Domain.Entities;

/// <summary>
/// A NASA CLPS (Commercial Lunar Payload Services) lunar mission.
/// Field names match the frontend's static data (src/data/missions.json)
/// so the API returns exactly what the React app already understands.
/// </summary>
public class Mission
{
    /// <summary>Slug id, e.g. "to2-ab". Same id the frontend uses in URLs.</summary>
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? TaskOrder { get; set; }
    public string? Provider { get; set; }
    public string? Lander { get; set; }

    /// <summary>completed | failed | partial | upcoming</summary>
    public string Status { get; set; } = "upcoming";

    public DateOnly? LaunchDate { get; set; }
    public string? LaunchDatePrecision { get; set; }
    public DateOnly? LandingDate { get; set; }
    public string? LandingDatePrecision { get; set; }

    // Landing site (flattened from the JSON's landingSite object)
    public string? LandingSiteName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    /// <summary>near side | far side | south polar</summary>
    public string? Region { get; set; }
    public bool CoordinatesApproximate { get; set; }

    public int? PayloadCountReported { get; set; }
    public string? Objective { get; set; }
    public string? OutcomeSummary { get; set; }
    public string? Notes { get; set; }
    public DateOnly? LastVerified { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Payload> Payloads { get; set; } = new List<Payload>();
    public ICollection<Source> Sources { get; set; } = new List<Source>();
}
