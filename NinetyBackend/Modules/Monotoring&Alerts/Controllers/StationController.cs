using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinetyGamingStationBackend.Application.DTOs.Stations;
using NinetyGamingStationBackend.Application.Interfaces;

namespace NinetyGamingStationBackend.Controllers;

[ApiController]
[Route("api/stations")]
[Authorize(Policy = "StationOperators")]
public sealed class StationController(IStationService service, IStationRepository stations, IBranchAccessService access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StationDto>>> GetAll([FromQuery] Guid? branchId, CancellationToken ct)
        {
        if (branchId.HasValue && !access.CanAccess(User, branchId.Value))
            return Forbid();
        var effectiveBranch = branchId;
        if (!string.Equals(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            var claim = User.FindFirst("branch_id")?.Value;
            if (!Guid.TryParse(claim, out var ownBranch)) return Forbid();
            effectiveBranch = ownBranch;
        }
        return Ok(await service.GetAllAsync(effectiveBranch, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StationDto>> GetById(Guid id, CancellationToken ct)
    {
        var station = await stations.GetByIdAsync(id, ct);
        if (station is null) return NotFound(new { error = "station_not_found" });
        if (!access.CanAccess(User, station.BranchId)) return Forbid();
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound(new { error = "station_not_found" }) : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "StationProvisioners")]
    public async Task<ActionResult<RegisterStationResultDto>> Register(RegisterStationDto dto, CancellationToken ct)
    {
        try
        {
            if (!access.CanAccess(User, dto.BranchId)) return Forbid();
            var result = await service.RegisterAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Station.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "StationProvisioners")]
    public async Task<IActionResult> Update(Guid id, UpdateStationDto dto, CancellationToken ct)
    {
        var station = await stations.GetByIdAsync(id, ct);
        if (station is null) return NotFound(new { error = "station_not_found" });
        if (!access.CanAccess(User, station.BranchId) || !access.CanAccess(User, dto.BranchId)) return Forbid();
        return await service.UpdateAsync(id, dto, ct)
            ? NoContent()
            : NotFound(new { error = "station_not_found" });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "StationProvisioners")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var station = await stations.GetByIdAsync(id, ct);
        if (station is null) return NotFound(new { error = "station_not_found" });
        if (!access.CanAccess(User, station.BranchId)) return Forbid();
        return await service.DeleteAsync(id, ct)
            ? NoContent()
            : NotFound(new { error = "station_not_found" });
    }
}
