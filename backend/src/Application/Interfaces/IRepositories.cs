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
