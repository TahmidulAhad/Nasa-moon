using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;
using NasaMoon.Application.Mappings;
using NasaMoon.Domain.Entities;

namespace NasaMoon.Application.Services;

public class MissionService : IMissionService
{
    private readonly IMissionRepository _missions;

    public MissionService(IMissionRepository missions)
    {
        _missions = missions;
    }

    public async Task<List<MissionDto>> GetMissionsAsync(MissionFilter filter, CancellationToken ct = default)
    {
        var missions = await _missions.GetAllAsync(
            filter.Search, filter.Status, filter.Provider, filter.LaunchYear, filter.Region, ct);
        return missions.Select(MissionMapper.ToDto).ToList();
    }

    public async Task<MissionDto?> GetMissionByIdAsync(string id, CancellationToken ct = default)
    {
        var mission = await _missions.GetByIdAsync(id, ct);
        return mission is null ? null : MissionMapper.ToDto(mission);
    }

    public async Task<MissionDto> CreateMissionAsync(CreateMissionDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Id and Name are required.");

        if (await _missions.ExistsAsync(dto.Id.Trim().ToLowerInvariant(), ct))
            throw new InvalidOperationException($"A mission with id '{dto.Id}' already exists.");

        var mission = MissionMapper.ToEntity(dto);
        mission = await _missions.AddAsync(mission, ct);
        return MissionMapper.ToDto(mission);
    }

    public async Task<MissionDto?> UpdateMissionAsync(string id, UpdateMissionDto dto, CancellationToken ct = default)
    {
        var mission = await _missions.GetByIdAsync(id, ct);
        if (mission is null) return null;

        MissionMapper.ApplyUpdate(mission, dto);
        await _missions.UpdateAsync(mission, ct);
        return MissionMapper.ToDto(mission);
    }

    public async Task<bool> DeleteMissionAsync(string id, CancellationToken ct = default)
        => await _missions.DeleteAsync(id, ct);
}

public class PayloadService : IPayloadService
{
    private readonly IPayloadRepository _payloads;

    public PayloadService(IPayloadRepository payloads)
    {
        _payloads = payloads;
    }

    public async Task<List<PayloadDto>> GetPayloadsByMissionAsync(string missionId, CancellationToken ct = default)
    {
        var payloads = await _payloads.GetByMissionIdAsync(missionId, ct);
        return payloads.Select(p => new PayloadDto
        {
            Id = p.Id,
            Name = p.Name,
            Organization = p.Organization,
            Purpose = p.Purpose,
            Type = p.Type
        }).ToList();
    }
}
