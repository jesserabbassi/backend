using Microsoft.AspNetCore.SignalR;
using NinetyGamingStationBackend.Application.DTOs.Monitoring;
using NinetyGamingStationBackend.Application.Interfaces;
using NinetyGamingStationBackend.Domain.Entities;
using NinetyGamingStationBackend.Domain.Enums;
using NinetyGamingStationBackend.Realtime;

namespace NinetyGamingStationBackend.Application.Services;

/// <summary>Processes incoming telemetry and exposes monitoring/health read operations.</summary>
public sealed class MonitoringService(
    ITelemetryRepository telemetry,
    IAlertService alertService,
    IPeripheralRepository peripherals,
    IStationRepository stations,
    AgentConnectionManager connections,
    IHubContext<MonitoringHub> hub) : IMonitoringService
{
    public async Task ProcessTelemetryAsync(Guid stationId, TelemetryDto dto, CancellationToken ct)
    {
        var station = await stations.GetByIdAsync(stationId, ct)
            ?? throw new KeyNotFoundException("Station not found.");

        var sample = new Telemetry
        {
            StationId = stationId,
            CpuUsage = Math.Clamp(dto.CpuUsage, 0, 100),
            GpuUsage = Math.Clamp(dto.GpuUsage, 0, 100),
            RamUsage = Math.Clamp(dto.RamUsage, 0, 100),
            CpuTemperature = Math.Clamp(dto.CpuTemperature, -50, 150),
            GpuTemperature = Math.Clamp(dto.GpuTemperature, -50, 150),
            FanSpeed = Math.Max(0, dto.FanSpeed),
            NetworkStatus = dto.NetworkStatus?.Trim(),
            Timestamp = dto.Timestamp == default ? DateTime.UtcNow : dto.Timestamp.ToUniversalTime()
        };

        await telemetry.AddAsync(sample, ct);

        station.LastHeartbeat = DateTime.UtcNow;
        if (station.Status is StationStatus.Offline or StationStatus.Unknown)
            station.Status = StationStatus.Online;
        await stations.UpdateAsync(station, ct);

        await hub.Clients.Groups(new[]
        {
            MonitoringHub.StationGroup(stationId),
            MonitoringHub.BranchGroup(station.BranchId)
        }).SendAsync("telemetry.updated", new
        {
            station_id = stationId,
            telemetry = Map(sample)
        }, ct);

        await EvaluateTelemetryAlertsAsync(sample, ct);
    }

    public async Task<TelemetryDto?> GetLatestTelemetryAsync(Guid stationId, CancellationToken ct)
    {
        var latest = await telemetry.GetLatestOneAsync(stationId, ct);
        return latest is null ? null : Map(latest);
    }

    public async Task<List<TelemetryDto>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct)
    {
        var boundedTake = Math.Clamp(take, 1, 500);
        return (await telemetry.GetLatestAsync(stationId, boundedTake, ct)).Select(Map).ToList();
    }

    public async Task<StationHealthDto?> GetHealthAsync(Guid stationId, CancellationToken ct)
    {
        var station = await stations.GetByIdAsync(stationId, ct);
        if (station is null) return null;

        var activeAlerts = await alertService.GetActiveAsync(stationId, null, ct);
        var age = DateTime.UtcNow - station.LastHeartbeat;
        var health = age.TotalSeconds <= 15 ? "HEALTHY" : age.TotalSeconds <= 60 ? "STALE" : "OFFLINE";

        return new StationHealthDto(
            station.Id,
            station.Status.ToString(),
            station.LastHeartbeat,
            connections.IsConnected(station.Id),
            health,
            activeAlerts.Count);
    }

    public async Task<List<AlertDto>> GetAlertsAsync(Guid? stationId, Guid? branchId, CancellationToken ct) =>
        (await alertService.GetActiveAsync(stationId, branchId, ct))
            .Select(a => new AlertDto(a.Id, a.StationId, a.Type.ToString(), a.Severity.ToString(),
                a.Message, a.CreatedAt, a.ResolvedAt))
            .ToList();

    public async Task ProcessPeripheralsAsync(
        Guid stationId, IReadOnlyList<PeripheralDto> snapshot, CancellationToken ct)
    {
        _ = await stations.GetByIdAsync(stationId, ct)
            ?? throw new KeyNotFoundException("Station not found.");

        var entities = snapshot.Select(p => new Peripheral
        {
            StationId = stationId,
            Type = p.Type.Trim(),
            Name = p.Name.Trim(),
            Status = p.Status.Trim(),
            LastSeen = p.LastSeen == default ? DateTime.UtcNow : p.LastSeen.ToUniversalTime()
        }).ToList();

        await peripherals.ReplaceSnapshotAsync(stationId, entities, ct);

        if (entities.Any(x => string.Equals(x.Status, "DISCONNECTED", StringComparison.OrdinalIgnoreCase)))
        {
            await alertService.CreateAlertAsync(stationId, AlertType.UsbChange, AlertSeverity.Warning,
                "One or more peripherals are disconnected.", ct);
        }
    }

    public async Task<List<PeripheralViewDto>> GetPeripheralsAsync(Guid stationId, CancellationToken ct) =>
        (await peripherals.GetAllAsync(stationId, ct))
            .Select(p => new PeripheralViewDto(p.Id, p.StationId, p.Type, p.Name, p.Status, p.LastSeen))
            .ToList();

    private async Task EvaluateTelemetryAlertsAsync(Telemetry sample, CancellationToken ct)
    {
        if (sample.CpuUsage >= 95)
            await alertService.CreateAlertAsync(sample.StationId, AlertType.HighCpu, AlertSeverity.Warning,
                $"CPU usage is {sample.CpuUsage:F1}%.", ct);

        if (sample.GpuUsage >= 95)
            await alertService.CreateAlertAsync(sample.StationId, AlertType.HighGpu, AlertSeverity.Warning,
                $"GPU usage is {sample.GpuUsage:F1}%.", ct);

        if (sample.GpuTemperature >= 90 || sample.CpuTemperature >= 95)
            await alertService.CreateAlertAsync(sample.StationId, AlertType.HighTemperature, AlertSeverity.Critical,
                $"Temperature threshold exceeded (CPU {sample.CpuTemperature:F1}°C, GPU {sample.GpuTemperature:F1}°C).", ct);

        if (sample.FanSpeed == 0 && (sample.CpuTemperature >= 70 || sample.GpuTemperature >= 70))
            await alertService.CreateAlertAsync(sample.StationId, AlertType.FanFailure, AlertSeverity.Critical,
                "Fan speed is zero while temperature is elevated.", ct);
    }

    private static TelemetryDto Map(Telemetry t) =>
        new(t.CpuUsage, t.GpuUsage, t.RamUsage, t.CpuTemperature, t.GpuTemperature,
            t.FanSpeed, t.NetworkStatus, t.Timestamp);
}
