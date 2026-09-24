using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.MonitoringAlerts.DTOs;
using NinetyBackend.Modules.MonitoringAlerts.Services;

namespace NinetyBackend.Modules.MonitoringAlerts.Controllers;

[ApiController]
[Route("api/monitoring")]
public class MonitoringController(IMonitoringService monitoringService, IAlertService alertService) : ControllerBase
{
    [HttpPost("stations/{stationId:guid}/telemetry")]
    public async Task<IActionResult> ProcessTelemetry(Guid stationId, [FromBody] TelemetryDto dto, CancellationToken ct)
    {
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
        var telemetry = await monitoringService.GetLatestTelemetryAsync(stationId, ct);
        return telemetry is null ? NotFound(new { message = "No telemetry found." }) : Ok(telemetry);
    }

    [HttpGet("stations/{stationId:guid}/telemetry")]
    public async Task<IActionResult> GetHistory(Guid stationId, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        var items = await monitoringService.GetHistoryAsync(stationId, take, ct);
        return Ok(items);
    }

    [HttpGet("stations/{stationId:guid}/alerts")]
    public async Task<IActionResult> GetAlerts(Guid stationId, CancellationToken ct)
    {
        var alerts = await alertService.GetAlertsAsync(stationId, ct);
        return Ok(alerts);
    }

    [HttpPost("alerts/{alertId:guid}/resolve")]
    public async Task<IActionResult> ResolveAlert(Guid alertId, CancellationToken ct)
    {
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
}
