using Microsoft.AspNetCore.Mvc;
using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;

namespace NasaMoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MissionsController : ControllerBase
{
    private readonly IMissionService _missions;

    public MissionsController(IMissionService missions)
    {
        _missions = missions;
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

    [HttpPost]
    public async Task<ActionResult<MissionDto>> Create([FromBody] CreateMissionDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _missions.CreateMissionAsync(dto, ct);
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

    [HttpPut("{id}")]
    public async Task<ActionResult<MissionDto>> Update(string id, [FromBody] UpdateMissionDto dto, CancellationToken ct)
    {
        var updated = await _missions.UpdateMissionAsync(id, dto, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await _missions.DeleteMissionAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
