namespace NasaMoon.Application.Dtos;

/// <summary>One saved snapshot (list view — no mission payload).</summary>
public class SnapshotDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MissionCount { get; set; }

    /// <summary>Owner of the snapshot, or null for legacy (ownerless, public) snapshots.</summary>
    public Guid? UserId { get; set; }

    /// <summary>True when the snapshot has no owner (created before accounts
    /// existed): legacy snapshots stay public, readable by everyone.</summary>
    public bool IsLegacy { get; set; }
}

/// <summary>One snapshot with its missions (detail view).</summary>
public class SnapshotDetailDto : SnapshotDto
{
    public List<MissionDto> Missions { get; set; } = new();
}

public class CreateSnapshotDto
{
    public string Label { get; set; } = string.Empty;

    /// <summary>"live" (current database) or "static" (bundled missions.json). Defaults to "live".</summary>
    public string? Source { get; set; }
}

/// <summary>One changed field on a mission that exists in both sets.</summary>
public class FieldChangeDto
{
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

/// <summary>A mission present in both sets with at least one field changed.</summary>
public class MissionChangeDto
{
    public string MissionId { get; set; } = string.Empty;
    public string MissionName { get; set; } = string.Empty;
    public List<FieldChangeDto> Changes { get; set; } = new();
}

/// <summary>Diff between two mission sets (snapshot vs snapshot, or snapshot vs live).</summary>
public class SnapshotDiffDto
{
    public string? FromId { get; set; }
    public string FromLabel { get; set; } = string.Empty;
    public string? ToId { get; set; }
    public string ToLabel { get; set; } = string.Empty;
    public List<MissionDto> Added { get; set; } = new();
    public List<MissionDto> Removed { get; set; } = new();
    public List<MissionChangeDto> Changed { get; set; } = new();
}
