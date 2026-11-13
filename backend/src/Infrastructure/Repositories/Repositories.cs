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
