using NasaMoon.Domain.Entities;

namespace NasaMoon.Application.Interfaces;

public interface IMissionRepository
{
    Task<List<Mission>> GetAllAsync(string? search, string? status, string? provider, int? launchYear, string? region, CancellationToken ct = default);
    Task<Mission?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Mission> AddAsync(Mission mission, CancellationToken ct = default);
    Task<bool> UpdateAsync(Mission mission, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<bool> ExistsAsync(string id, CancellationToken ct = default);
}

public interface IPayloadRepository
{
    Task<List<Payload>> GetByMissionIdAsync(string missionId, CancellationToken ct = default);
    Task<Payload?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Payload> AddAsync(Payload payload, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public interface ISourceRepository
{
    Task<List<Source>> GetByMissionIdAsync(string missionId, CancellationToken ct = default);
}

public interface ISnapshotRepository
{
    Task<List<DataSnapshot>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Snapshots visible to the caller: admin sees all; a researcher sees
    /// their own plus legacy (ownerless) ones; anonymous sees legacy only.
    /// </summary>
    Task<List<DataSnapshot>> GetVisibleAsync(Guid? userId, bool isAdmin, CancellationToken ct = default);
    Task<DataSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DataSnapshot> AddAsync(DataSnapshot snapshot, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Reads the bundled static dataset (frontend src/data/missions.json copy).</summary>
public interface IStaticMissionJsonProvider
{
    /// <summary>Raw missions JSON, or null when the file cannot be found.</summary>
    Task<string?> GetMissionsJsonAsync(CancellationToken ct = default);
}
