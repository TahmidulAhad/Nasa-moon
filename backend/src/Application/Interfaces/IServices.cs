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
    Task<SnapshotDto> CreateSnapshotAsync(CreateSnapshotDto dto, CancellationToken ct = default);
    Task<List<SnapshotDto>> ListSnapshotsAsync(CancellationToken ct = default);
    Task<SnapshotDetailDto?> GetSnapshotAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteSnapshotAsync(Guid id, CancellationToken ct = default);

    /// <summary>Diff two mission sets. toId is a snapshot id, or "live" for the current database.</summary>
    Task<SnapshotDiffDto?> CompareAsync(Guid fromId, string toId, CancellationToken ct = default);
}
