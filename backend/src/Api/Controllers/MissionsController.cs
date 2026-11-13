using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;

namespace NasaMoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MissionsController : ControllerBase
{
    private readonly IMissionService _missions;
    private readonly IAuditLogService _audit;

    public MissionsController(IMissionService missions, IAuditLogService audit)
    {
        _missions = missions;
        _audit = audit;
    }

    /// <summary>List missions with free-text search and filters (status / provider / launch year / region).</summary>
    [HttpGet]
    public async Task<ActionResult<List<MissionDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? provider,
        [FromQuery] int? launchYear,
        [FromQuery] string? region,
        CancellationToken ct)
    {
        var filter = new MissionFilter
        {
            Search = search,
            Status = status,
            Provider = provider,
            LaunchYear = launchYear,
            Region = region
        };
        return Ok(await _missions.GetMissionsAsync(filter, ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MissionDto>> GetById(string id, CancellationToken ct)
    {
        var mission = await _missions.GetMissionByIdAsync(id, ct);
        return mission is null ? NotFound() : Ok(mission);
    }

    /// <summary>Create a mission — Admin only. Writes are audit-logged.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MissionDto>> Create([FromBody] CreateMissionDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _missions.CreateMissionAsync(dto, ct);
            await _audit.LogAsync(User.GetUserId(), "mission.create", "mission", created.Id, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>Update a mission — Admin only. Writes are audit-logged.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MissionDto>> Update(string id, [FromBody] UpdateMissionDto dto, CancellationToken ct)
    {
        var updated = await _missions.UpdateMissionAsync(id, dto, ct);
        if (updated is null) return NotFound();
        await _audit.LogAsync(User.GetUserId(), "mission.update", "mission", id, ct);
        return Ok(updated);
    }

    /// <summary>Delete a mission — Admin only. Writes are audit-logged.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await _missions.DeleteMissionAsync(id, ct);
        if (deleted)
            await _audit.LogAsync(User.GetUserId(), "mission.delete", "mission", id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
