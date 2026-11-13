using NasaMoon.Application.Dtos;

namespace NasaMoon.Application.Interfaces;

public interface IMissionService
{
    Task<List<MissionDto>> GetMissionsAsync(MissionFilter filter, CancellationToken ct = default);
    Task<MissionDto?> GetMissionByIdAsync(string id, CancellationToken ct = default);
    Task<MissionDto> CreateMissionAsync(CreateMissionDto dto, CancellationToken ct = default);
    Task<MissionDto?> UpdateMissionAsync(string id, UpdateMissionDto dto, CancellationToken ct = default);
    Task<bool> DeleteMissionAsync(string id, CancellationToken ct = default);
}

public interface IPayloadService
{
    Task<List<PayloadDto>> GetPayloadsByMissionAsync(string missionId, CancellationToken ct = default);
}

public interface ISnapshotService
{
    /// <summary>Create a snapshot owned by userId (null = legacy/public, only used by tests/seed).</summary>
    Task<SnapshotDto> CreateSnapshotAsync(CreateSnapshotDto dto, Guid? userId, CancellationToken ct = default);

    /// <summary>
    /// Visible snapshots: admin sees all; a researcher sees their own plus
    /// legacy (ownerless) ones; anonymous callers see legacy only.
    /// </summary>
    Task<List<SnapshotDto>> ListSnapshotsAsync(Guid? userId, bool isAdmin, CancellationToken ct = default);
    Task<SnapshotDetailDto?> GetSnapshotAsync(Guid id, Guid? userId, bool isAdmin, CancellationToken ct = default);
    Task<bool> DeleteSnapshotAsync(Guid id, Guid? userId, bool isAdmin, CancellationToken ct = default);

    /// <summary>Diff two mission sets. toId is a snapshot id, or "live" for the current database.
    /// Both snapshots must be visible to the caller.</summary>
    Task<SnapshotDiffDto?> CompareAsync(Guid fromId, string toId, Guid? userId, bool isAdmin, CancellationToken ct = default);
}
