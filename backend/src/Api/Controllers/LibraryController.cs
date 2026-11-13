using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;

namespace NasaMoon.Api.Controllers;

/// <summary>
/// The logged-in researcher's personal library: saved missions with their
/// own notes and tags, plus a file export (JSON/CSV) for their local drive.
/// Every row is scoped to the caller — other researchers' rows are invisible.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LibraryController : ControllerBase
{
    private readonly ILibraryService _library;
    private readonly IExportService _export;

    public LibraryController(ILibraryService library, IExportService export)
    {
        _library = library;
        _export = export;
    }

    /// <summary>My saved missions, newest first. ?search= matches mission name, note or tag.</summary>
    [HttpGet]
    public async Task<ActionResult<List<SavedItemDto>>> List([FromQuery] string? search, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await _library.ListSavedAsync(userId.Value, search, ct));
    }

    /// <summary>Save a mission to my library (idempotent — saving twice returns the same row).</summary>
    [HttpPost]
    public async Task<ActionResult<SavedItemDto>> Save([FromBody] SaveMissionDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        try
        {
            var saved = await _library.SaveMissionAsync(userId.Value, dto, ct);
            return CreatedAtAction(nameof(List), new { }, saved);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Change my note / replace my tags on a saved item.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SavedItemDto>> Update(Guid id, [FromBody] UpdateSavedItemDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        try
        {
            var updated = await _library.UpdateSavedItemAsync(userId.Value, id, dto, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var deleted = await _library.DeleteSavedItemAsync(userId.Value, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Download my whole library as a file (?format=json|csv, default json).</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string format = "json", CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        try
        {
            var (bytes, contentType, fileName) = await _export.ExportLibraryAsync(userId.Value, format, ct);
            return File(bytes, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
