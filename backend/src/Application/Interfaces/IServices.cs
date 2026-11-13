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
