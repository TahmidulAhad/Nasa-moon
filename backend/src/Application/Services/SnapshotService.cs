using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;
using NasaMoon.Application.Mappings;
using NasaMoon.Domain.Entities;

namespace NasaMoon.Application.Services;

public class SnapshotService : ISnapshotService
{
    private readonly ISnapshotRepository _snapshots;
    private readonly IMissionRepository _missions;
    private readonly IStaticMissionJsonProvider _staticData;

    public SnapshotService(
        ISnapshotRepository snapshots,
        IMissionRepository missions,
        IStaticMissionJsonProvider staticData)
    {
        _snapshots = snapshots;
        _missions = missions;
        _staticData = staticData;
    }

    public async Task<SnapshotDto> CreateSnapshotAsync(CreateSnapshotDto dto, Guid? userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Label))
            throw new ArgumentException("Label is required.");

        var source = (dto.Source ?? "live").Trim().ToLowerInvariant();
        if (source is not ("live" or "static"))
            throw new ArgumentException("Source must be 'live' or 'static'.");

        List<MissionDto> missions;
        if (source == "static")
        {
            var json = await _staticData.GetMissionsJsonAsync(ct);
            if (json is null)
                throw new InvalidOperationException("Static dataset (missions.json) is not available.");
            missions = SnapshotMapper.DeserializeMissions(json);
            if (missions.Count == 0)
                throw new InvalidOperationException("Static dataset is empty.");
        }
        else
        {
            var entities = await _missions.GetAllAsync(null, null, null, null, null, ct);
            missions = entities.Select(MissionMapper.ToDto).ToList();
        }

        var snapshot = new DataSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Label = dto.Label.Trim(),
            Source = source,
            CreatedAt = DateTime.UtcNow,
            MissionsJson = SnapshotMapper.SerializeMissions(missions)
        };
        snapshot = await _snapshots.AddAsync(snapshot, ct);
        return SnapshotMapper.ToSummaryDto(snapshot);
    }

    public async Task<List<SnapshotDto>> ListSnapshotsAsync(Guid? userId, bool isAdmin, CancellationToken ct = default)
    {
        var snapshots = await _snapshots.GetVisibleAsync(userId, isAdmin, ct);
        return snapshots
            .OrderByDescending(s => s.CreatedAt)
            .Select(SnapshotMapper.ToSummaryDto)
            .ToList();
    }

    public async Task<SnapshotDetailDto?> GetSnapshotAsync(Guid id, Guid? userId, bool isAdmin, CancellationToken ct = default)
    {
        var snapshot = await _snapshots.GetByIdAsync(id, ct);
        if (snapshot is null || !IsVisible(snapshot, userId, isAdmin)) return null;
        return SnapshotMapper.ToDetailDto(snapshot);
    }

    public async Task<bool> DeleteSnapshotAsync(Guid id, Guid? userId, bool isAdmin, CancellationToken ct = default)
    {
        var snapshot = await _snapshots.GetByIdAsync(id, ct);
        // Legacy snapshots have no owner: only an admin may delete them.
        if (snapshot is null) return false;
        if (!isAdmin && (snapshot.UserId is null || snapshot.UserId != userId)) return false;
        return await _snapshots.DeleteAsync(id, ct);
    }

    public async Task<SnapshotDiffDto?> CompareAsync(Guid fromId, string toId, Guid? userId, bool isAdmin, CancellationToken ct = default)
    {
        var from = await _snapshots.GetByIdAsync(fromId, ct);
        if (from is null || !IsVisible(from, userId, isAdmin)) return null;

        List<MissionDto> toMissions;
        string toLabel;
        Guid? toGuid;
        if (toId.Trim().Equals("live", StringComparison.OrdinalIgnoreCase))
        {
            var entities = await _missions.GetAllAsync(null, null, null, null, null, ct);
            toMissions = entities.Select(MissionMapper.ToDto).ToList();
            toLabel = "Live data";
            toGuid = null;
        }
        else
        {
            if (!Guid.TryParse(toId, out var parsedToId)) return null;
            var to = await _snapshots.GetByIdAsync(parsedToId, ct);
            if (to is null || !IsVisible(to, userId, isAdmin)) return null;
            toMissions = SnapshotMapper.DeserializeMissions(to.MissionsJson);
            toLabel = to.Label;
            toGuid = to.Id;
        }

        var fromMissions = SnapshotMapper.DeserializeMissions(from.MissionsJson);
        var diff = ComputeDiff(fromMissions, toMissions);
        diff.FromId = from.Id.ToString();
        diff.FromLabel = from.Label;
        diff.ToId = toGuid?.ToString();
        diff.ToLabel = toLabel;
        return diff;
    }

    /// <summary>
    /// Visibility rule (matches the repository's GetVisibleAsync): admins see
    /// everything; owners see their own snapshots; everyone sees legacy
    /// (ownerless) snapshots.
    /// </summary>
    public static bool IsVisible(DataSnapshot snapshot, Guid? userId, bool isAdmin)
        => isAdmin || snapshot.UserId is null || (userId.HasValue && snapshot.UserId == userId.Value);

    /// <summary>
    /// Pure diff between two mission sets, keyed on mission id.
    /// Added = in "to" only; Removed = in "from" only; Changed = in both
    /// with at least one compared field different.
    /// </summary>
    public static SnapshotDiffDto ComputeDiff(List<MissionDto> from, List<MissionDto> to)
    {
        var fromById = from.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);
        var toById = to.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);

        var result = new SnapshotDiffDto
        {
            Added = to.Where(m => !fromById.ContainsKey(m.Id)).ToList(),
            Removed = from.Where(m => !toById.ContainsKey(m.Id)).ToList()
        };

        foreach (var oldMission in from)
        {
            if (!toById.TryGetValue(oldMission.Id, out var newMission)) continue;
            var changes = CompareMission(oldMission, newMission);
            if (changes.Count == 0) continue;
            result.Changed.Add(new MissionChangeDto
            {
                MissionId = oldMission.Id,
                MissionName = oldMission.Name,
                Changes = changes
            });
        }

        return result;
    }

    /// <summary>Field-by-field comparison (the fields researchers care about).</summary>
    private static List<FieldChangeDto> CompareMission(MissionDto oldM, MissionDto newM)
    {
        var changes = new List<FieldChangeDto>();
        void Check(string field, string? oldValue, string? newValue)
        {
            if (!string.Equals(oldValue ?? string.Empty, newValue ?? string.Empty, StringComparison.Ordinal))
                changes.Add(new FieldChangeDto { Field = field, OldValue = oldValue, NewValue = newValue });
        }

        Check("name", oldM.Name, newM.Name);
        Check("status", oldM.Status, newM.Status);
        Check("launchDate", oldM.LaunchDate, newM.LaunchDate);
        Check("landingDate", oldM.LandingDate, newM.LandingDate);
        Check("landingSite", oldM.LandingSite?.Name, newM.LandingSite?.Name);
        Check("region", oldM.LandingSite?.Region, newM.LandingSite?.Region);
        Check("provider", oldM.Provider, newM.Provider);
        Check("lander", oldM.Lander, newM.Lander);
        Check("payloads", oldM.Payloads.Count.ToString(), newM.Payloads.Count.ToString());
        Check("objective", oldM.Objective, newM.Objective);
        return changes;
    }
}
