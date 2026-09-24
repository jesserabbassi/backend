using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinetyGamingStationBackend.Application.DTOs.Monitoring;
using NinetyGamingStationBackend.Application.Interfaces;

namespace NinetyGamingStationBackend.Controllers;

[ApiController]
[Route("api/monitoring")]
[Authorize(Policy = "StationOperators")]
public sealed class MonitoringController(IMonitoringService service, IAlertService alertService, IStationRepository stations, IBranchAccessService access) : ControllerBase
{
    [HttpGet("stations/{stationId:guid}/telemetry/latest")]
    public async Task<ActionResult<TelemetryDto>> GetLatestTelemetry(Guid stationId, CancellationToken ct)
    {
        var station = await stations.GetByIdAsync(stationId, ct);
        if (station is null) return NotFound(new { error = "station_not_found" });
        if (!access.CanAccess(User, station.BranchId)) return Forbid();
        var t = await service.GetLatestTelemetryAsync(stationId, ct);
        return t is null ? NotFound(new { error = "telemetry_not_found" }) : Ok(t);
    }

    [HttpGet("stations/{stationId:guid}/telemetry")]
    public async Task<ActionResult<IReadOnlyList<TelemetryDto>>> GetHistory(Guid stationId, [FromQuery] int take = 100, CancellationToken ct = default)
        {
        var station = await stations.GetByIdAsync(stationId, ct);
        if (station is null) return NotFound(new { error = "station_not_found" });
        if (!access.CanAccess(User, station.BranchId)) return Forbid();
        return Ok(await service.GetHistoryAsync(stationId, take, ct));
    }

    [HttpGet("stations/{stationId:guid}/health")]
    public async Task<ActionResult<StationHealthDto>> GetHealth(Guid stationId, CancellationToken ct)
    {
        var station = await stations.GetByIdAsync(stationId, ct);
        if (station is null) return NotFound(new { error = "station_not_found" });
        if (!access.CanAccess(User, station.BranchId)) return Forbid();
        var health = await service.GetHealthAsync(stationId, ct);
        return health is null ? NotFound(new { error = "station_not_found" }) : Ok(health);
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<IReadOnlyList<AlertDto>>> GetAlerts([FromQuery] Guid? stationId, CancellationToken ct)
    {
        Guid? branchId = null;
        if (stationId.HasValue)
        {
            var station = await stations.GetByIdAsync(stationId.Value, ct);
            if (station is null) return NotFound(new { error = "station_not_found" });
            if (!access.CanAccess(User, station.BranchId)) return Forbid();
        }
        else if (!User.IsInRole("SuperAdmin"))
        {
            if (!Guid.TryParse(User.FindFirst("branch_id")?.Value, out var userBranchId))
                return Forbid();
            branchId = userBranchId;
        }

        return Ok(await service.GetAlertsAsync(stationId, branchId, ct));
    }

    [HttpGet("stations/{stationId:guid}/peripherals")]
    public async Task<IActionResult> GetPeripherals(Guid stationId, CancellationToken ct)
        {
        var station = await stations.GetByIdAsync(stationId, ct);
        if (station is null) return NotFound(new { error = "station_not_found" });
        if (!access.CanAccess(User, station.BranchId)) return Forbid();
        return Ok(await service.GetPeripheralsAsync(stationId, ct));
    }

    [HttpPost("alerts/{alertId:guid}/resolve")]
    public async Task<IActionResult> ResolveAlert(Guid alertId, CancellationToken ct)
    {
        var target = await alertService.GetByIdAsync(alertId, ct);
        if (target is null || target.ResolvedAt is not null)
            return NotFound(new { error = "alert_not_found" });
        var station = await stations.GetByIdAsync(target.StationId, ct);
        if (station is null) return NotFound(new { error = "alert_not_found" });
        if (!access.CanAccess(User, station.BranchId)) return Forbid();
        try { await alertService.ResolveAlertAsync(alertId, ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(new { error = "alert_not_found" }); }
    }
}
