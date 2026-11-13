using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;
using NasaMoon.Domain.Entities;

namespace NasaMoon.Application.Services;

public class LibraryService : ILibraryService
{
    private readonly ISavedItemRepository _savedItems;
    private readonly ICollectionRepository _collections;
    private readonly IMissionRepository _missions;

    public LibraryService(
        ISavedItemRepository savedItems,
        ICollectionRepository collections,
        IMissionRepository missions)
    {
        _savedItems = savedItems;
        _collections = collections;
        _missions = missions;
    }

    public async Task<SavedItemDto> SaveMissionAsync(Guid userId, SaveMissionDto dto, CancellationToken ct = default)
    {
        var missionId = (dto.MissionId ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(missionId))
            throw new ArgumentException("MissionId is required.");
        if (!await _missions.ExistsAsync(missionId, ct))
            throw new ArgumentException($"No mission with id '{missionId}'.");

        // Idempotent: saving twice returns the existing row.
        var existing = await _savedItems.GetByUserAndMissionAsync(userId, missionId, ct);
        if (existing is not null) return await ToDtoAsync(existing, ct);

        var item = new SavedItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MissionId = missionId,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            CreatedAt = DateTime.UtcNow,
            Tags = CleanTags(dto.Tags).Select(t => new SavedItemTag { Tag = t }).ToList()
        };
        item = await _savedItems.AddAsync(item, ct);
        return await ToDtoAsync(item, ct);
    }

    public async Task<List<SavedItemDto>> ListSavedAsync(Guid userId, string? search, CancellationToken ct = default)
    {
        var items = await _savedItems.GetByUserAsync(userId, search, ct);
        var dtos = new List<SavedItemDto>();
        foreach (var item in items)
        {
            // Search also matches the mission's name (not stored on the row).
            if (!string.IsNullOrWhiteSpace(search) &&
                (item.Note is null || !item.Note.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                !item.Tags.Any(t => t.Tag.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                !item.MissionId.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var mission = await _missions.GetByIdAsync(item.MissionId, ct);
                if (mission is null || !mission.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            dtos.Add(await ToDtoAsync(item, ct));
        }
        return dtos;
    }

    public async Task<SavedItemDto?> UpdateSavedItemAsync(Guid userId, Guid id, UpdateSavedItemDto dto, CancellationToken ct = default)
    {
        var item = await _savedItems.GetByIdAsync(id, ct);
        if (item is null || item.UserId != userId) return null;

        if (dto.Note is not null)
            item.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();

        if (dto.Tags is not null)
        {
            item.Tags.Clear();
            foreach (var tag in CleanTags(dto.Tags))
                item.Tags.Add(new SavedItemTag { SavedItemId = item.Id, Tag = tag });
        }

        await _savedItems.UpdateAsync(item, ct);
        return await ToDtoAsync(item, ct);
    }

    public async Task<bool> DeleteSavedItemAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var item = await _savedItems.GetByIdAsync(id, ct);
        if (item is null || item.UserId != userId) return false;
        return await _savedItems.DeleteAsync(id, ct);
    }

    public async Task<List<CollectionDto>> ListCollectionsAsync(Guid userId, CancellationToken ct = default)
    {
        var collections = await _collections.GetByUserAsync(userId, ct);
        return collections.Select(c => new CollectionDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ItemCount = c.Items.Count,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<CollectionDto> CreateCollectionAsync(Guid userId, CreateCollectionDto dto, CancellationToken ct = default)
    {
        var name = (dto.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Collection name is required.");

        var collection = new Collection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        collection = await _collections.AddAsync(collection, ct);
        return new CollectionDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            ItemCount = 0,
            CreatedAt = collection.CreatedAt
        };
    }

    public async Task<CollectionDetailDto?> GetCollectionAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var collection = await _collections.GetByIdAsync(id, ct);
        if (collection is null || collection.UserId != userId) return null;

        var missionIds = await _collections.GetItemMissionIdsAsync(id, ct);
        var items = new List<SavedItemDto>();
        foreach (var missionId in missionIds)
        {
            var mission = await _missions.GetByIdAsync(missionId, ct);
            var saved = await _savedItems.GetByUserAndMissionAsync(userId, missionId, ct);
            items.Add(new SavedItemDto
            {
                Id = saved?.Id ?? Guid.Empty,
                MissionId = missionId,
                MissionName = mission?.Name,
                MissionStatus = mission?.Status,
                MissionProvider = mission?.Provider,
                Note = saved?.Note,
                Tags = saved is null ? new() : saved.Tags.Select(t => t.Tag).OrderBy(t => t).ToList(),
                CreatedAt = saved?.CreatedAt ?? collection.CreatedAt
            });
        }

        return new CollectionDetailDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            ItemCount = items.Count,
            CreatedAt = collection.CreatedAt,
            Items = items
        };
    }

    public async Task<CollectionDto?> UpdateCollectionAsync(Guid userId, Guid id, UpdateCollectionDto dto, CancellationToken ct = default)
    {
        var collection = await _collections.GetByIdAsync(id, ct);
        if (collection is null || collection.UserId != userId) return null;
        var name = (dto.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Collection name is required.");

        collection.Name = name;
        collection.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        await _collections.UpdateAsync(collection, ct);
        return new CollectionDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            ItemCount = collection.Items.Count,
            CreatedAt = collection.CreatedAt
        };
    }

    public async Task<bool> DeleteCollectionAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var collection = await _collections.GetByIdAsync(id, ct);
        if (collection is null || collection.UserId != userId) return false;
        return await _collections.DeleteAsync(id, ct);
    }

    public async Task<bool> AddToCollectionAsync(Guid userId, Guid collectionId, string missionId, CancellationToken ct = default)
    {
        var collection = await _collections.GetByIdAsync(collectionId, ct);
        if (collection is null || collection.UserId != userId) return false;
        missionId = missionId.Trim().ToLowerInvariant();
        if (!await _missions.ExistsAsync(missionId, ct)) return false;
        return await _collections.AddItemAsync(collectionId, missionId, ct);
    }

    public async Task<bool> RemoveFromCollectionAsync(Guid userId, Guid collectionId, string missionId, CancellationToken ct = default)
    {
        var collection = await _collections.GetByIdAsync(collectionId, ct);
        if (collection is null || collection.UserId != userId) return false;
        return await _collections.RemoveItemAsync(collectionId, missionId.Trim().ToLowerInvariant(), ct);
    }

    private async Task<SavedItemDto> ToDtoAsync(SavedItem item, CancellationToken ct)
    {
        var mission = await _missions.GetByIdAsync(item.MissionId, ct);
        return new SavedItemDto
        {
            Id = item.Id,
            MissionId = item.MissionId,
            MissionName = mission?.Name,
            MissionStatus = mission?.Status,
            MissionProvider = mission?.Provider,
            Note = item.Note,
            Tags = item.Tags.Select(t => t.Tag).OrderBy(t => t).ToList(),
            CreatedAt = item.CreatedAt
        };
    }

    private static List<string> CleanTags(List<string>? tags)
        => (tags ?? new())
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0 && t.Length <= 50)
            .Distinct()
            .OrderBy(t => t)
            .ToList();
}
