using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;

namespace NasaMoon.Api.Controllers;

/// <summary>
/// The logged-in researcher's own collections (folders of missions).
/// Private only in Phase 1 — no sharing.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private readonly ILibraryService _library;
    private readonly IExportService _export;

    public CollectionsController(ILibraryService library, IExportService export)
    {
        _library = library;
        _export = export;
    }

    [HttpGet]
    public async Task<ActionResult<List<CollectionDto>>> List(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await _library.ListCollectionsAsync(userId.Value, ct));
    }

    [HttpPost]
    public async Task<ActionResult<CollectionDto>> Create([FromBody] CreateCollectionDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        try
        {
            var created = await _library.CreateCollectionAsync(userId.Value, dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CollectionDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var collection = await _library.GetCollectionAsync(userId.Value, id, ct);
        return collection is null ? NotFound() : Ok(collection);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CollectionDto>> Update(Guid id, [FromBody] UpdateCollectionDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        try
        {
            var updated = await _library.UpdateCollectionAsync(userId.Value, id, dto, ct);
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
        var deleted = await _library.DeleteCollectionAsync(userId.Value, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddCollectionItemDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var added = await _library.AddToCollectionAsync(userId.Value, id, dto.MissionId, ct);
        return added ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}/items/{missionId}")]
    public async Task<IActionResult> RemoveItem(Guid id, string missionId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var removed = await _library.RemoveFromCollectionAsync(userId.Value, id, missionId, ct);
        return removed ? NoContent() : NotFound();
    }

    /// <summary>Download one collection as a file (?format=json|csv, default json).</summary>
    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, [FromQuery] string format = "json", CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        try
        {
            var result = await _export.ExportCollectionAsync(userId.Value, id, format, ct);
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
