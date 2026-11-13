using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;

namespace NasaMoon.Api.Controllers;

/// <summary>
/// Research snapshots: save the mission dataset at a point in time and
/// compare two snapshots (or a snapshot vs the live data) field by field.
/// Snapshots belong to the researcher who created them; legacy snapshots
/// (created before accounts existed) stay public. Admins see all.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SnapshotsController : ControllerBase
{
    private readonly ISnapshotService _snapshots;
    private readonly IExportService _export;
    private readonly IAuditLogService _audit;

    public SnapshotsController(ISnapshotService snapshots, IExportService export, IAuditLogService audit)
    {
        _snapshots = snapshots;
        _export = export;
        _audit = audit;
    }

    /// <summary>
    /// Save a snapshot of the current dataset ("live" DB or "static" bundled data).
    /// Login required — the snapshot is owned by the caller.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<SnapshotDto>> Create([FromBody] CreateSnapshotDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        try
        {
            var created = await _snapshots.CreateSnapshotAsync(dto, userId.Value, ct);
            await _audit.LogAsync(userId.Value, "snapshot.create", "snapshot", created.Id.ToString(), ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// List visible snapshots, newest first: the caller's own plus legacy
    /// (ownerless) ones; admins see all. Works without login (legacy only).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SnapshotDto>>> List(CancellationToken ct)
        => Ok(await _snapshots.ListSnapshotsAsync(User.GetUserId(), User.IsAdmin(), ct));

    /// <summary>
    /// Compare two mission sets. fromId is a snapshot id; toId is a snapshot
    /// id or "live" for the current database. Both snapshots must be visible
    /// to the caller.
    /// </summary>
    [HttpGet("compare")]
    public async Task<ActionResult<SnapshotDiffDto>> Compare(
        [FromQuery] Guid fromId,
        [FromQuery] string toId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(toId))
            return BadRequest("toId is required (a snapshot id or 'live').");

        var diff = await _snapshots.CompareAsync(fromId, toId, User.GetUserId(), User.IsAdmin(), ct);
        return diff is null ? NotFound("Snapshot not found.") : Ok(diff);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SnapshotDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var snapshot = await _snapshots.GetSnapshotAsync(id, User.GetUserId(), User.IsAdmin(), ct);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    /// <summary>Delete a snapshot — its owner, or an admin. Legacy snapshots: admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var deleted = await _snapshots.DeleteSnapshotAsync(id, userId.Value, User.IsAdmin(), ct);
        if (deleted)
            await _audit.LogAsync(userId.Value, "snapshot.delete", "snapshot", id.ToString(), ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Download one snapshot's missions (?format=json|csv). Must be visible to the caller.</summary>
    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, [FromQuery] string format = "json", CancellationToken ct = default)
    {
        var visible = await _snapshots.GetSnapshotAsync(id, User.GetUserId(), User.IsAdmin(), ct);
        if (visible is null) return NotFound();
        try
        {
            var result = await _export.ExportSnapshotAsync(id, format, ct);
            return result is null
                ? NotFound()
                : File(result.Value.Bytes, result.Value.ContentType, result.Value.FileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
