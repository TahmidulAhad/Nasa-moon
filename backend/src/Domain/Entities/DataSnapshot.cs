namespace NasaMoon.Domain.Entities;

/// <summary>
/// A saved copy of the mission dataset at a point in time, so visitors can
/// research what changed between two moments (or between a saved moment and
/// the live data). The missions are stored as one JSON array in exactly the
/// shape the API returns (MissionDto, camelCase) — the same shape as the
/// frontend's static src/data/missions.json — so any snapshot can be read
/// back and diffed without touching the live tables.
/// </summary>
public class DataSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Owner of the snapshot. Null = legacy snapshot created before accounts
    /// existed: those stay public, readable by everyone (including visitors).
    /// A snapshot with an owner is visible only to that owner (and admins).
    /// </summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Human label, e.g. "Before the March update".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Where the data came from: "live" (current database) or "static" (bundled missions.json).</summary>
    public string Source { get; set; } = "live";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Full missions JSON array (MissionDto shape, camelCase).</summary>
    public string MissionsJson { get; set; } = "[]";
}
