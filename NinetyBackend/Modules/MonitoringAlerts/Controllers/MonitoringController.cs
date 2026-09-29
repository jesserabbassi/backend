using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.MonitoringAlerts.DTOs;
using NinetyBackend.Modules.MonitoringAlerts.Services;
using NinetyBackend.Modules.Stations.Services;

namespace NinetyBackend.Modules.MonitoringAlerts.Controllers;

[ApiController]
[Authorize]
[Route("api/monitoring")]
public class MonitoringController(IMonitoringService monitoringService, IAlertService alertService, IAgentService agentService) : ControllerBase
{
    [HttpPost("stations/{stationId:guid}/telemetry")]
    public async Task<IActionResult> ProcessTelemetry(Guid stationId, [FromBody] TelemetryDto dto, CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor") && !await IsAssignedAgentAsync(stationId))
            return Forbid();

        try
        {
            await monitoringService.ProcessTelemetryAsync(stationId, dto, ct);
            return Ok(new { message = "Telemetry processed." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("stations/{stationId:guid}/telemetry/latest")]
    public async Task<IActionResult> GetLatestTelemetry(Guid stationId, CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor")) return Forbid();
        var telemetry = await monitoringService.GetLatestTelemetryAsync(stationId, ct);
        return telemetry is null ? NotFound(new { message = "No telemetry found." }) : Ok(telemetry);
    }

    [HttpGet("stations/{stationId:guid}/telemetry")]
    public async Task<IActionResult> GetHistory(Guid stationId, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor")) return Forbid();
        take = Math.Clamp(take, 1, 500);
        var items = await monitoringService.GetHistoryAsync(stationId, take, ct);
        return Ok(items);
    }

    [HttpGet("stations/{stationId:guid}/alerts")]
    public async Task<IActionResult> GetAlerts(Guid stationId, CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor")) return Forbid();
        var alerts = await alertService.GetAlertsAsync(stationId, ct);
        return Ok(alerts);
    }

    [HttpPost("alerts/{alertId:guid}/resolve")]
    public async Task<IActionResult> ResolveAlert(Guid alertId, CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor")) return Forbid();
        try
        {
            await alertService.ResolveAlertAsync(alertId, ct);
            return Ok(new { message = "Alert resolved." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private async Task<bool> IsAssignedAgentAsync(Guid stationId)
    {
        var claim = User.FindFirstValue("agent_id");
        return Guid.TryParse(claim, out var agentId)
            && (await agentService.GetByIdAsync(agentId))?.StationId == stationId;
    }
}
