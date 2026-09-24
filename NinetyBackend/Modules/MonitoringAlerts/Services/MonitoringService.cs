using NinetyBackend.Modules.MonitoringAlerts.DTOs;
using NinetyBackend.Modules.MonitoringAlerts.Models;
using NinetyBackend.Modules.MonitoringAlerts.Repositories;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Stations.Repositories;

namespace NinetyBackend.Modules.MonitoringAlerts.Services;

public interface IMonitoringService
{
    Task ProcessTelemetryAsync(Guid stationId, TelemetryDto dto, CancellationToken ct = default);
    Task<TelemetryDto?> GetLatestTelemetryAsync(Guid stationId, CancellationToken ct = default);
    Task<List<TelemetryDto>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct = default);
    Task<List<AlertDto>> GetAlertsAsync(Guid stationId, CancellationToken ct = default);
}

public class MonitoringService(
    ITelemetryRepository telemetryRepository,
    IAlertRepository alertRepository,
    IStationRepository stationRepository) : IMonitoringService
{
    public async Task ProcessTelemetryAsync(Guid stationId, TelemetryDto dto, CancellationToken ct = default)
    {
        var station = await stationRepository.GetByIdAsync(stationId);
        if (station is null)
        {
            throw new KeyNotFoundException("Station not found.");
        }

        var telemetry = new Telemetry
        {
            StationId = stationId,
            CpuUsage = Math.Clamp(dto.CpuUsage, 0, 100),
            GpuUsage = Math.Clamp(dto.GpuUsage, 0, 100),
            RamUsage = Math.Clamp(dto.RamUsage, 0, 100),
            CpuTemperature = Math.Clamp(dto.CpuTemperature, -50, 150),
            GpuTemperature = Math.Clamp(dto.GpuTemperature, -50, 150),
            FanSpeed = Math.Max(0, dto.FanSpeed),
            NetworkStatus = dto.NetworkStatus,
            Timestamp = dto.Timestamp == default ? DateTime.UtcNow : dto.Timestamp
        };

        await telemetryRepository.AddAsync(telemetry, ct);

        station.LastSeenAt = DateTime.UtcNow;
        station.Status = StationStatus.Available;
        await stationRepository.UpdateAsync(station);

        if (dto.CpuUsage >= 95 || dto.GpuUsage >= 95 || dto.CpuTemperature >= 90 || dto.GpuTemperature >= 90)
        {
            await CreateAlertAsync(stationId, AlertType.HighTemperature, AlertSeverity.Warning, "Telemetry threshold exceeded.", ct);
        }
    }

    public async Task<TelemetryDto?> GetLatestTelemetryAsync(Guid stationId, CancellationToken ct = default)
    {
        var telemetry = await telemetryRepository.GetLatestAsync(stationId, ct);
        return telemetry is null ? null : Map(telemetry);
    }

    public async Task<List<TelemetryDto>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct = default)
    {
        var items = await telemetryRepository.GetHistoryAsync(stationId, take, ct);
        return items.Select(Map).ToList();
    }

    public async Task<List<AlertDto>> GetAlertsAsync(Guid stationId, CancellationToken ct = default)
    {
        var alerts = await alertRepository.GetActiveAsync(stationId, ct);
        return alerts.Select(a => new AlertDto(a.Id, a.StationId, a.Type.ToString(), a.Severity.ToString(), a.Message, a.CreatedAt, a.ResolvedAt)).ToList();
    }

    private async Task CreateAlertAsync(Guid stationId, AlertType type, AlertSeverity severity, string message, CancellationToken ct)
    {
        var active = await alertRepository.GetActiveAsync(stationId, ct);
        if (active.Any(a => a.Type == type && a.ResolvedAt is null))
        {
            return;
        }

        await alertRepository.AddAsync(new Alert
        {
            StationId = stationId,
            Type = type,
            Severity = severity,
            Message = message,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }

    private static TelemetryDto Map(Telemetry telemetry) => new(
        telemetry.StationId,
        telemetry.CpuUsage,
        telemetry.GpuUsage,
        telemetry.RamUsage,
        telemetry.CpuTemperature,
        telemetry.GpuTemperature,
        telemetry.FanSpeed,
        telemetry.NetworkStatus,
        telemetry.Timestamp);
}

public interface IAlertService
{
    Task<List<AlertDto>> GetAlertsAsync(Guid stationId, CancellationToken ct = default);
    Task<AlertDto?> GetByIdAsync(Guid alertId, CancellationToken ct = default);
    Task ResolveAlertAsync(Guid alertId, CancellationToken ct = default);
}

public class AlertService(IAlertRepository alertRepository) : IAlertService
{
    public async Task<List<AlertDto>> GetAlertsAsync(Guid stationId, CancellationToken ct = default)
    {
        var alerts = await alertRepository.GetActiveAsync(stationId, ct);
        return alerts.Select(a => new AlertDto(a.Id, a.StationId, a.Type.ToString(), a.Severity.ToString(), a.Message, a.CreatedAt, a.ResolvedAt)).ToList();
    }

    public async Task<AlertDto?> GetByIdAsync(Guid alertId, CancellationToken ct = default)
    {
        var alert = await alertRepository.GetByIdAsync(alertId, ct);
        return alert is null ? null : new AlertDto(alert.Id, alert.StationId, alert.Type.ToString(), alert.Severity.ToString(), alert.Message, alert.CreatedAt, alert.ResolvedAt);
    }

    public async Task ResolveAlertAsync(Guid alertId, CancellationToken ct = default)
    {
        var alert = await alertRepository.GetByIdAsync(alertId, ct);
        if (alert is null)
        {
            throw new KeyNotFoundException("Alert not found.");
        }

        alert.ResolvedAt ??= DateTime.UtcNow;
        await alertRepository.UpdateAsync(alert, ct);
    }
}
