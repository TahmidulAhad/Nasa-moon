using System.Text;
using System.Text.Json;
using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;
using NasaMoon.Application.Mappings;

namespace NasaMoon.Application.Services;

/// <summary>
/// Exports a researcher's own data as a downloadable file (local drive).
/// JSON for programs, CSV for spreadsheets. Nothing is stored server-side.
/// </summary>
public class ExportService : IExportService
{
    private readonly ISavedItemRepository _savedItems;
    private readonly ICollectionRepository _collections;
    private readonly IMissionRepository _missions;
    private readonly ISnapshotRepository _snapshots;

    public ExportService(
        ISavedItemRepository savedItems,
        ICollectionRepository collections,
        IMissionRepository missions,
        ISnapshotRepository snapshots)
    {
        _savedItems = savedItems;
        _collections = collections;
        _missions = missions;
        _snapshots = snapshots;
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> ExportLibraryAsync(Guid userId, string format, CancellationToken ct = default)
    {
        format = NormalizeFormat(format);
        var items = await _savedItems.GetByUserAsync(userId, null, ct);
        var collections = await _collections.GetByUserAsync(userId, ct);

        var rows = new List<ExportRow>();
        foreach (var item in items)
        {
            var mission = await _missions.GetByIdAsync(item.MissionId, ct);
            rows.Add(new ExportRow
            {
                MissionId = item.MissionId,
                MissionName = mission?.Name ?? string.Empty,
                Status = mission?.Status ?? string.Empty,
                Provider = mission?.Provider ?? string.Empty,
                LaunchDate = mission?.LaunchDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                LandingSite = mission?.LandingSiteName ?? string.Empty,
                Region = mission?.Region ?? string.Empty,
                Payloads = mission is null ? 0 : mission.Payloads.Count,
                Note = item.Note ?? string.Empty,
                Tags = string.Join("; ", item.Tags.Select(t => t.Tag)),
                CollectionNames = string.Join("; ", collections
                    .Where(c => c.Items.Any(i => i.MissionId == item.MissionId))
                    .Select(c => c.Name)),
                SavedAt = item.CreatedAt
            });
        }

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd");
        return format == "csv"
            ? (Encoding.UTF8.GetBytes(ToCsv(rows)), "text/csv", $"nasa-moon-library-{stamp}.csv")
            : (Encoding.UTF8.GetBytes(ToJson(new
            {
                exportedAt = DateTime.UtcNow,
                items = rows,
                collections = collections.Select(c => new
                {
                    c.Name,
                    c.Description,
                    missions = c.Items.Select(i => i.MissionId).ToList()
                })
            })), "application/json", $"nasa-moon-library-{stamp}.json");
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)?> ExportCollectionAsync(Guid userId, Guid collectionId, string format, CancellationToken ct = default)
    {
        format = NormalizeFormat(format);
        var collection = await _collections.GetByIdAsync(collectionId, ct);
        if (collection is null || collection.UserId != userId) return null;

        var missionIds = await _collections.GetItemMissionIdsAsync(collectionId, ct);
        var rows = new List<ExportRow>();
        foreach (var missionId in missionIds)
        {
            var mission = await _missions.GetByIdAsync(missionId, ct);
            var saved = await _savedItems.GetByUserAndMissionAsync(userId, missionId, ct);
            rows.Add(new ExportRow
            {
                MissionId = missionId,
                MissionName = mission?.Name ?? string.Empty,
                Status = mission?.Status ?? string.Empty,
                Provider = mission?.Provider ?? string.Empty,
                LaunchDate = mission?.LaunchDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                LandingSite = mission?.LandingSiteName ?? string.Empty,
                Region = mission?.Region ?? string.Empty,
                Payloads = mission is null ? 0 : mission.Payloads.Count,
                Note = saved?.Note ?? string.Empty,
                Tags = saved is null ? string.Empty : string.Join("; ", saved.Tags.Select(t => t.Tag)),
                CollectionNames = collection.Name,
                SavedAt = saved?.CreatedAt ?? collection.CreatedAt
            });
        }

        var safeName = Slug(collection.Name);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd");
        return format == "csv"
            ? (Encoding.UTF8.GetBytes(ToCsv(rows)), "text/csv", $"nasa-moon-collection-{safeName}-{stamp}.csv")
            : (Encoding.UTF8.GetBytes(ToJson(new
            {
                collection = collection.Name,
                description = collection.Description,
                exportedAt = DateTime.UtcNow,
                items = rows
            })), "application/json", $"nasa-moon-collection-{safeName}-{stamp}.json");
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)?> ExportSnapshotAsync(Guid snapshotId, string format, CancellationToken ct = default)
    {
        format = NormalizeFormat(format);
        var snapshot = await _snapshots.GetByIdAsync(snapshotId, ct);
        if (snapshot is null) return null;

        var missions = SnapshotMapper.DeserializeMissions(snapshot.MissionsJson);
        var rows = missions.Select(m => new ExportRow
        {
            MissionId = m.Id,
            MissionName = m.Name,
            Status = m.Status,
            Provider = m.Provider ?? string.Empty,
            LaunchDate = m.LaunchDate ?? string.Empty,
            LandingSite = m.LandingSite?.Name ?? string.Empty,
            Region = m.LandingSite?.Region ?? string.Empty,
            Payloads = m.Payloads.Count,
            Note = string.Empty,
            Tags = string.Empty,
            CollectionNames = string.Empty,
            SavedAt = snapshot.CreatedAt
        }).ToList();

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd");
        return format == "csv"
            ? (Encoding.UTF8.GetBytes(ToCsv(rows)), "text/csv", $"nasa-moon-snapshot-{stamp}.csv")
            : (Encoding.UTF8.GetBytes(ToJson(new
            {
                snapshot = snapshot.Label,
                source = snapshot.Source,
                createdAt = snapshot.CreatedAt,
                items = rows
            })), "application/json", $"nasa-moon-snapshot-{stamp}.json");
    }

    private static string NormalizeFormat(string format)
    {
        format = (format ?? "json").Trim().ToLowerInvariant();
        if (format is not ("json" or "csv"))
            throw new ArgumentException("format must be 'json' or 'csv'.");
        return format;
    }

    private static string ToJson(object value)
        => JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

    private static string ToCsv(List<ExportRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("missionId,missionName,status,provider,launchDate,landingSite,region,payloads,note,tags,collections,savedAt");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                Csv(r.MissionId), Csv(r.MissionName), Csv(r.Status), Csv(r.Provider),
                Csv(r.LaunchDate), Csv(r.LandingSite), Csv(r.Region), r.Payloads.ToString(),
                Csv(r.Note), Csv(r.Tags), Csv(r.CollectionNames), Csv(r.SavedAt.ToString("yyyy-MM-dd"))
            }));
        }
        return sb.ToString();
    }

    private static string Csv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private static string Slug(string name)
    {
        var slug = new string(name.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        return slug.Length == 0 ? "collection" : slug[..Math.Min(slug.Length, 40)];
    }

    private sealed class ExportRow
    {
        public string MissionId { get; set; } = string.Empty;
        public string MissionName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string LaunchDate { get; set; } = string.Empty;
        public string LandingSite { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public int Payloads { get; set; }
        public string Note { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string CollectionNames { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }
    }
}
