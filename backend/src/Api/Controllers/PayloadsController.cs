using Microsoft.AspNetCore.Mvc;
using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;

namespace NasaMoon.Api.Controllers;

[ApiController]
[Route("api/missions/{missionId}/[controller]")]
public class PayloadsController : ControllerBase
{
    private readonly IPayloadService _payloads;

    public PayloadsController(IPayloadService payloads)
    {
        _payloads = payloads;
    }

    /// <summary>All payloads carried by one mission.</summary>
    [HttpGet]
    public async Task<ActionResult<List<PayloadDto>>> GetByMission(string missionId, CancellationToken ct)
        => Ok(await _payloads.GetPayloadsByMissionAsync(missionId, ct));
}
