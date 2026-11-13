using Microsoft.AspNetCore.Mvc;
using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;

namespace NasaMoon.Api.Controllers;

/// <summary>
/// Research snapshots: save the mission dataset at a point in time and
/// compare two snapshots (or a snapshot vs the live data) field by field.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SnapshotsController : ControllerBase
{
    private readonly ISnapshotService _snapshots;

    public SnapshotsController(ISnapshotService snapshots)
    {
        _snapshots = snapshots;
    }

    /// <summary>Save a snapshot of the current dataset ("live" DB or "static" bundled data).</summary>
    [HttpPost]
    public async Task<ActionResult<SnapshotDto>> Create([FromBody] CreateSnapshotDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _snapshots.CreateSnapshotAsync(dto, ct);
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

    /// <summary>List snapshots, newest first.</summary>
    [HttpGet]
    public async Task<ActionResult<List<SnapshotDto>>> List(CancellationToken ct)
        => Ok(await _snapshots.ListSnapshotsAsync(ct));

    /// <summary>
    /// Compare two mission sets. fromId is a snapshot id; toId is a snapshot
    /// id or "live" for the current database.
    /// </summary>
    [HttpGet("compare")]
    public async Task<ActionResult<SnapshotDiffDto>> Compare(
        [FromQuery] Guid fromId,
        [FromQuery] string toId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(toId))
            return BadRequest("toId is required (a snapshot id or 'live').");

        var diff = await _snapshots.CompareAsync(fromId, toId, ct);
        return diff is null ? NotFound("Snapshot not found.") : Ok(diff);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SnapshotDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var snapshot = await _snapshots.GetSnapshotAsync(id, ct);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _snapshots.DeleteSnapshotAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
