namespace NasaMoon.Application.Dtos;

/// <summary>One mission in the caller's personal library, with their note/tags
/// and a small mission summary so the library page needs no extra calls.</summary>
public class SavedItemDto
{
    public Guid Id { get; set; }
    public string MissionId { get; set; } = string.Empty;
    public string? MissionName { get; set; }
    public string? MissionStatus { get; set; }
    public string? MissionProvider { get; set; }
    public string? Note { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class SaveMissionDto
{
    public string MissionId { get; set; } = string.Empty;
    public string? Note { get; set; }
    public List<string>? Tags { get; set; }
}

public class UpdateSavedItemDto
{
    /// <summary>Null = keep the current note; empty string clears it.</summary>
    public string? Note { get; set; }

    /// <summary>Null = keep current tags; otherwise replaces the whole tag list.</summary>
    public List<string>? Tags { get; set; }
}

public class CollectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>A collection with its missions (mission summaries, same shape as library rows).</summary>
public class CollectionDetailDto : CollectionDto
{
    public List<SavedItemDto> Items { get; set; } = new();
}

public class CreateCollectionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateCollectionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AddCollectionItemDto
{
    public string MissionId { get; set; } = string.Empty;
}

/// <summary>One audit-log row (admin view).</summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public DateTime CreatedAt { get; set; }
}
