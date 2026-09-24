using NinetyBackend.Modules.MonitoringAlerts.Models;

namespace NinetyBackend.Modules.MonitoringAlerts.Repositories;

public interface ITelemetryRepository
{
    Task AddAsync(Telemetry telemetry, CancellationToken ct = default);
    Task<List<Telemetry>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct = default);
    Task<Telemetry?> GetLatestAsync(Guid stationId, CancellationToken ct = default);
}

public interface IAlertRepository
{
    Task<Alert> AddAsync(Alert alert, CancellationToken ct = default);
    Task<List<Alert>> GetActiveAsync(Guid? stationId, CancellationToken ct = default);
    Task<Alert?> GetByIdAsync(Guid alertId, CancellationToken ct = default);
    Task UpdateAsync(Alert alert, CancellationToken ct = default);
}
