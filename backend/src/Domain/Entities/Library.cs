namespace NasaMoon.Domain.Entities;

/// <summary>
/// One mission saved to a researcher's personal library, with their own
/// note and tags. The mission itself stays public; only this row is private.
/// </summary>
public class SavedItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Mission slug id, e.g. "to2-ab". No hard FK: the saved row
    /// survives even if an admin later deletes the mission.</summary>
    public string MissionId { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SavedItemTag> Tags { get; set; } = new List<SavedItemTag>();
}

/// <summary>One free-text tag on a saved item. (SavedItemId, Tag) is unique.</summary>
public class SavedItemTag
{
    public Guid SavedItemId { get; set; }
    public SavedItem SavedItem { get; set; } = null!;

    public string Tag { get; set; } = string.Empty;
}

/// <summary>
/// A researcher's own folder of missions, e.g. "South Pole Missions".
/// Private by default — no sharing in Phase 1.
/// </summary>
public class Collection
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CollectionItem> Items { get; set; } = new List<CollectionItem>();
}

/// <summary>One mission inside a collection. (CollectionId, MissionId) is unique.</summary>
public class CollectionItem
{
    public Guid CollectionId { get; set; }
    public Collection Collection { get; set; } = null!;

    public string MissionId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Audit trail: who changed shared data, when. Written for mission
/// create/update/delete and snapshot create/delete.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Null only for system/seed actions.</summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>e.g. "mission.create", "mission.update", "snapshot.delete".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>e.g. "mission", "snapshot".</summary>
    public string Entity { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
