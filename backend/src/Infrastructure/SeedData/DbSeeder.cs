using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NasaMoon.Domain.Entities;

namespace NasaMoon.Infrastructure.SeedData;

/// <summary>
/// Seeds the database from the frontend's own static data
/// (src/data/missions.json) so the API serves the exact same 15 missions
/// and 50 payloads the site already shows. Runs once, only on an empty DB.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(NasaMoonDbContext db, CancellationToken ct = default)
    {
        if (await db.Missions.AnyAsync(ct)) return;

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "missions.json");
        if (!File.Exists(jsonPath))
        {
            // Fallback for `dotnet run` from the Api project folder.
            jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "SeedData", "missions.json");
        }
        if (!File.Exists(jsonPath)) return;

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var records = JsonSerializer.Deserialize<List<MissionJson>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (records is null) return;

        foreach (var r in records)
        {
            var mission = new Mission
            {
                Id = r.Id,
                Name = r.Name,
                TaskOrder = r.TaskOrder,
                Provider = r.Provider,
                Lander = r.Lander,
                Status = (r.Status ?? "upcoming").ToLowerInvariant(),
                LaunchDate = ParseDate(r.LaunchDate),
                LaunchDatePrecision = r.LaunchDatePrecision,
                LandingDate = ParseDate(r.LandingDate),
                LandingDatePrecision = r.LandingDatePrecision,
                LandingSiteName = r.LandingSite?.Name,
                Latitude = r.LandingSite?.Latitude,
                Longitude = r.LandingSite?.Longitude,
                Region = r.LandingSite?.Region,
                CoordinatesApproximate = r.LandingSite?.CoordinatesApproximate ?? false,
                PayloadCountReported = r.PayloadCountReported,
                Objective = r.Objective,
                OutcomeSummary = r.OutcomeSummary,
                Notes = r.Notes,
                LastVerified = ParseDate(r.LastVerified),
                Payloads = (r.Payloads ?? new()).Select(p => new Payload
                {
                    Name = p.Name,
                    Organization = p.Organization,
                    Purpose = p.Purpose,
                    Type = p.Type
                }).ToList(),
                Sources = (r.Sources ?? new()).Select(s => new Source
                {
                    Label = s.Label,
                    Url = s.Url,
                    Accessed = ParseDate(s.Accessed)
                }).ToList()
            };
            await db.Missions.AddAsync(mission, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, out var d) ? d : null;

    // JSON shapes — mirror src/data/missions.json exactly.
    private sealed record MissionJson(
        string Id,
        string Name,
        string? TaskOrder,
        string? Provider,
        string? Lander,
        string? Status,
        string? LaunchDate,
        string? LaunchDatePrecision,
        string? LandingDate,
        string? LandingDatePrecision,
        LandingSiteJson? LandingSite,
        List<PayloadJson>? Payloads,
        int? PayloadCountReported,
        string? Objective,
        string? OutcomeSummary,
        string? Notes,
        List<SourceJson>? Sources,
        string? LastVerified);

    private sealed record LandingSiteJson(
        string? Name,
        double? Latitude,
        double? Longitude,
        string? Region,
        bool CoordinatesApproximate);

    private sealed record PayloadJson(
        string Name,
        string? Organization,
        string? Purpose,
        string? Type);

    private sealed record SourceJson(
        string Label,
        string Url,
        string? Accessed);
}
