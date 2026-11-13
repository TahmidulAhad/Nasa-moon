using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;

namespace NasaMoon.Api.Controllers;

/// <summary>Admin-only views: researcher accounts and the audit trail.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IAuditLogService _audit;

    public AdminController(IUserRepository users, IAuditLogService audit)
    {
        _users = users;
        _audit = audit;
    }

    /// <summary>All researcher accounts (password hashes are never returned).</summary>
    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> Users(CancellationToken ct)
    {
        var users = await _users.GetAllAsync(ct);
        return Ok(users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.DisplayName,
            Role = u.Role,
            CreatedAt = u.CreatedAt
        }).ToList());
    }

    /// <summary>Newest audit entries first (?count=, default 100).</summary>
    [HttpGet("audit-logs")]
    public async Task<ActionResult<List<AuditLogDto>>> AuditLogs([FromQuery] int count = 100, CancellationToken ct = default)
    {
        count = Math.Clamp(count, 1, 500);
        return Ok(await _audit.GetRecentAsync(count, ct));
    }
}
