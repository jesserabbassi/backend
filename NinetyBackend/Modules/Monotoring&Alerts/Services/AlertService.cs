using Microsoft.AspNetCore.SignalR;
using NinetyGamingStationBackend.Application.Interfaces;
using NinetyGamingStationBackend.Domain.Entities;
using NinetyGamingStationBackend.Domain.Enums;
using NinetyGamingStationBackend.Realtime;

namespace NinetyGamingStationBackend.Application.Services;

/// <summary>Owns alert lifecycle: deduplication, persistence, resolution and realtime notifications.</summary>
public sealed class AlertService(
    IAlertRepository alerts,
    IStationRepository stations,
    IHubContext<MonitoringHub> hub) : IAlertService
{
    public async Task<Alert> CreateAlertAsync(
        Guid stationId, AlertType type, AlertSeverity severity, string message, CancellationToken ct)
    {
        var station = await stations.GetByIdAsync(stationId, ct)
            ?? throw new KeyNotFoundException("Station not found.");

        // Avoid opening duplicate active alerts of the same type for a station.
        var active = await alerts.GetActiveAsync(stationId, null, ct);
        var existing = active.FirstOrDefault(a => a.Type == type);
        if (existing is not null) return existing;

        var created = await alerts.AddAsync(new Alert
        {
            StationId = stationId,
            Type = type,
            Severity = severity,
            Message = message.Trim()
        }, ct);

        await NotifyAsync(station, "alert.created", new
        {
            alert_id = created.Id,
            station_id = stationId,
            type = created.Type.ToString(),
            severity = created.Severity.ToString(),
            message = created.Message,
            created_at = created.CreatedAt
        }, ct);

        return created;
    }

    public Task<Alert?> GetByIdAsync(Guid alertId, CancellationToken ct) =>
        alerts.GetByIdAsync(alertId, ct);

    public async Task ResolveAlertAsync(Guid alertId, CancellationToken ct)
    {
        var alert = await alerts.GetByIdAsync(alertId, ct);
        if (alert is null || alert.ResolvedAt is not null)
            throw new KeyNotFoundException("Active alert not found.");

        alert.ResolvedAt = DateTime.UtcNow;
        await alerts.UpdateAsync(alert, ct);

        var station = await stations.GetByIdAsync(alert.StationId, ct);
        if (station is not null)
        {
            await NotifyAsync(station, "alert.resolved", new
            {
                alert_id = alert.Id,
                station_id = alert.StationId,
                resolved_at = alert.ResolvedAt
            }, ct);
        }
    }

    public Task<List<Alert>> GetActiveAsync(Guid? stationId, Guid? branchId, CancellationToken ct) =>
        alerts.GetActiveAsync(stationId, branchId, ct);

    private Task NotifyAsync(GamingStation station, string eventName, object payload, CancellationToken ct) =>
        hub.Clients.Groups(new[]
        {
            MonitoringHub.StationGroup(station.Id),
            MonitoringHub.BranchGroup(station.BranchId)
        }).SendAsync(eventName, payload, ct);
}
