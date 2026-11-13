using Microsoft.EntityFrameworkCore;
using NasaMoon.Application.Interfaces;
using NasaMoon.Domain.Entities;

namespace NasaMoon.Infrastructure.Repositories;

public class MissionRepository : IMissionRepository
{
    private readonly NasaMoonDbContext _db;

    public MissionRepository(NasaMoonDbContext db)
    {
        _db = db;
    }

    public async Task<List<Mission>> GetAllAsync(string? search, string? status, string? provider, int? launchYear, string? region, CancellationToken ct = default)
    {
        var query = _db.Missions
            .Include(m => m.Payloads)
            .Include(m => m.Sources)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                m.Name.ToLower().Contains(term) ||
                (m.Lander != null && m.Lander.ToLower().Contains(term)) ||
                (m.Provider != null && m.Provider.ToLower().Contains(term)) ||
                (m.LandingSiteName != null && m.LandingSiteName.ToLower().Contains(term)) ||
                (m.TaskOrder != null && m.TaskOrder.ToLower().Contains(term)) ||
                m.Payloads.Any(p => p.Name.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(m => m.Status == status.Trim().ToLower());

        if (!string.IsNullOrWhiteSpace(provider))
            query = query.Where(m => m.Provider != null && m.Provider.ToLower() == provider.Trim().ToLower());

        if (launchYear.HasValue)
            query = query.Where(m => m.LaunchDate.HasValue && m.LaunchDate.Value.Year == launchYear.Value);

        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(m => m.Region != null && m.Region.ToLower() == region.Trim().ToLower());

        return await query.OrderBy(m => m.LaunchDate).ThenBy(m => m.Name).ToListAsync(ct);
    }

    public async Task<Mission?> GetByIdAsync(string id, CancellationToken ct = default)
        => await _db.Missions
            .Include(m => m.Payloads)
            .Include(m => m.Sources)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<Mission> AddAsync(Mission mission, CancellationToken ct = default)
    {
        await _db.Missions.AddAsync(mission, ct);
        await _db.SaveChangesAsync(ct);
        return mission;
    }

    public async Task<bool> UpdateAsync(Mission mission, CancellationToken ct = default)
    {
        _db.Missions.Update(mission);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var mission = await _db.Missions.FindAsync(new object[] { id }, ct);
        if (mission is null) return false;
        _db.Missions.Remove(mission);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken ct = default)
        => await _db.Missions.AnyAsync(m => m.Id == id, ct);
}

public class PayloadRepository : IPayloadRepository
{
    private readonly NasaMoonDbContext _db;

    public PayloadRepository(NasaMoonDbContext db)
    {
        _db = db;
    }

    public async Task<List<Payload>> GetByMissionIdAsync(string missionId, CancellationToken ct = default)
        => await _db.Payloads.Where(p => p.MissionId == missionId).OrderBy(p => p.Name).ToListAsync(ct);

    public async Task<Payload?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.Payloads.FindAsync(new object[] { id }, ct);

    public async Task<Payload> AddAsync(Payload payload, CancellationToken ct = default)
    {
        await _db.Payloads.AddAsync(payload, ct);
        await _db.SaveChangesAsync(ct);
        return payload;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var payload = await _db.Payloads.FindAsync(new object[] { id }, ct);
        if (payload is null) return false;
        _db.Payloads.Remove(payload);
        return await _db.SaveChangesAsync(ct) > 0;
    }
}

public class SourceRepository : ISourceRepository
{
    private readonly NasaMoonDbContext _db;

    public SourceRepository(NasaMoonDbContext db)
    {
        _db = db;
    }

    public async Task<List<Source>> GetByMissionIdAsync(string missionId, CancellationToken ct = default)
        => await _db.Sources.Where(s => s.MissionId == missionId).OrderBy(s => s.Label).ToListAsync(ct);
}

public class SnapshotRepository : ISnapshotRepository
{
    private readonly NasaMoonDbContext _db;

    public SnapshotRepository(NasaMoonDbContext db)
    {
        _db = db;
    }

    public async Task<List<DataSnapshot>> GetAllAsync(CancellationToken ct = default)
        => await _db.Snapshots.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);

    public async Task<List<DataSnapshot>> GetVisibleAsync(Guid? userId, bool isAdmin, CancellationToken ct = default)
    {
        if (isAdmin)
            return await _db.Snapshots.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);

        // Own snapshots + legacy (ownerless) ones; anonymous callers (userId null) see legacy only.
        return await _db.Snapshots
            .Where(s => s.UserId == null || (userId.HasValue && s.UserId == userId.Value))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<DataSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Snapshots.FindAsync(new object[] { id }, ct);

    public async Task<DataSnapshot> AddAsync(DataSnapshot snapshot, CancellationToken ct = default)
    {
        await _db.Snapshots.AddAsync(snapshot, ct);
        await _db.SaveChangesAsync(ct);
        return snapshot;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var snapshot = await _db.Snapshots.FindAsync(new object[] { id }, ct);
        if (snapshot is null) return false;
        _db.Snapshots.Remove(snapshot);
        return await _db.SaveChangesAsync(ct) > 0;
    }
}

public class UserRepository : IUserRepository
{
    private readonly NasaMoonDbContext _db;

    public UserRepository(NasaMoonDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Users.FindAsync(new object[] { id }, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _db.Users.CountAsync(ct);

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<List<User>> GetAllAsync(CancellationToken ct = default)
        => await _db.Users.OrderBy(u => u.CreatedAt).ToListAsync(ct);

    public async Task<bool> UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        return await _db.SaveChangesAsync(ct) > 0;
    }
}

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly NasaMoonDbContext _db;

    public RefreshTokenRepository(NasaMoonDbContext db)
    {
        _db = db;
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _db.RefreshTokens.AddAsync(token, ct);
        await _db.SaveChangesAsync(ct);
        return token;
    }

    public async Task<bool> UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _db.RefreshTokens.Update(token);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var token in active) token.RevokedAt = DateTime.UtcNow;
        return await _db.SaveChangesAsync(ct);
    }
}

public class SavedItemRepository : ISavedItemRepository
{
    private readonly NasaMoonDbContext _db;

    public SavedItemRepository(NasaMoonDbContext db)
    {
        _db = db;
    }

    public async Task<List<SavedItem>> GetByUserAsync(Guid userId, string? search, CancellationToken ct = default)
    {
        var query = _db.SavedItems
            .Include(s => s.Tags)
            .Where(s => s.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.MissionId.ToLower().Contains(term) ||
                (s.Note != null && s.Note.ToLower().Contains(term)) ||
                s.Tags.Any(t => t.Tag.ToLower().Contains(term)));
        }

        return await query.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
    }

    public async Task<SavedItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.SavedItems
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<SavedItem?> GetByUserAndMissionAsync(Guid userId, string missionId, CancellationToken ct = default)
        => await _db.SavedItems
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.MissionId == missionId, ct);

    public async Task<SavedItem> AddAsync(SavedItem item, CancellationToken ct = default)
    {
        await _db.SavedItems.AddAsync(item, ct);
        await _db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<bool> UpdateAsync(SavedItem item, CancellationToken ct = default)
    {
        _db.SavedItems.Update(item);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _db.SavedItems.FindAsync(new object[] { id }, ct);
        if (item is null) return false;
        _db.SavedItems.Remove(item);
        return await _db.SaveChangesAsync(ct) > 0;
    }
}

public class CollectionRepository : ICollectionRepository
{
    private readonly NasaMoonDbContext _db;

    public CollectionRepository(NasaMoonDbContext db)
    {
        _db = db;
    }

    public async Task<List<Collection>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.Collections
            .Include(c => c.Items)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<Collection?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Collections
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Collection> AddAsync(Collection collection, CancellationToken ct = default)
    {
        await _db.Collections.AddAsync(collection, ct);
        await _db.SaveChangesAsync(ct);
        return collection;
    }

    public async Task<bool> UpdateAsync(Collection collection, CancellationToken ct = default)
    {
        _db.Collections.Update(collection);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var collection = await _db.Collections.FindAsync(new object[] { id }, ct);
        if (collection is null) return false;
        _db.Collections.Remove(collection);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> AddItemAsync(Guid collectionId, string missionId, CancellationToken ct = default)
    {
        var exists = await _db.CollectionItems.AnyAsync(
            i => i.CollectionId == collectionId && i.MissionId == missionId, ct);
        if (exists) return true;
        await _db.CollectionItems.AddAsync(new CollectionItem
        {
            CollectionId = collectionId,
            MissionId = missionId,
            CreatedAt = DateTime.UtcNow
        }, ct);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> RemoveItemAsync(Guid collectionId, string missionId, CancellationToken ct = default)
    {
        var item = await _db.CollectionItems.FirstOrDefaultAsync(
            i => i.CollectionId == collectionId && i.MissionId == missionId, ct);
        if (item is null) return false;
        _db.CollectionItems.Remove(item);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<List<string>> GetItemMissionIdsAsync(Guid collectionId, CancellationToken ct = default)
        => await _db.CollectionItems
            .Where(i => i.CollectionId == collectionId)
            .OrderBy(i => i.CreatedAt)
            .Select(i => i.MissionId)
            .ToListAsync(ct);
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly NasaMoonDbContext _db;

    public AuditLogRepository(NasaMoonDbContext db)
    {
        _db = db;
    }

    public async Task<AuditLog> AddAsync(AuditLog log, CancellationToken ct = default)
    {
        await _db.AuditLogs.AddAsync(log, ct);
        await _db.SaveChangesAsync(ct);
        return log;
    }

    public async Task<List<AuditLog>> GetRecentAsync(int count, CancellationToken ct = default)
        => await _db.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
}

/// <summary>Reads the bundled static dataset (same file DbSeeder seeds from).</summary>
public class StaticMissionJsonProvider : IStaticMissionJsonProvider
{
    public async Task<string?> GetMissionsJsonAsync(CancellationToken ct = default)
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "missions.json");
        if (!File.Exists(jsonPath))
        {
            // Fallback for `dotnet run` from the Api project folder.
            jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "SeedData", "missions.json");
        }
        if (!File.Exists(jsonPath)) return null;
        return await File.ReadAllTextAsync(jsonPath, ct);
    }
}
