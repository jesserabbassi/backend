using NinetyGamingStationBackend.Domain.Entities;
using NinetyGamingStationBackend.Domain.Enums;
using NinetyGamingStationBackend.Application.DTOs.Monitoring;
using NinetyGamingStationBackend.Application.DTOs.RemoteControl;
using NinetyGamingStationBackend.Application.DTOs.Stations;
using NinetyGamingStationBackend.Realtime;

namespace NinetyGamingStationBackend.Application.Interfaces;

public interface IBranchAccessService
{
    bool CanAccess(System.Security.Claims.ClaimsPrincipal user, Guid branchId);
}

public interface IStationRepository
{
    Task<List<GamingStation>> GetAllAsync(Guid? branchId, CancellationToken ct);
    Task<GamingStation?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<GamingStation?> GetByMachineIdAsync(string machineId, CancellationToken ct);
    Task<GamingStation> AddAsync(GamingStation station, CancellationToken ct);
    Task UpdateAsync(GamingStation station, CancellationToken ct);
    Task DeleteAsync(GamingStation station, CancellationToken ct);
}

public interface ITelemetryRepository
{
    Task AddAsync(Telemetry telemetry, CancellationToken ct);
    Task<List<Telemetry>> GetLatestAsync(Guid stationId, int take, CancellationToken ct);
    Task<Telemetry?> GetLatestOneAsync(Guid stationId, CancellationToken ct);
}

public interface IAlertRepository
{
    Task<Alert> AddAsync(Alert alert, CancellationToken ct);
    Task<List<Alert>> GetActiveAsync(Guid? stationId, Guid? branchId, CancellationToken ct);
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken ct);
    Task UpdateAsync(Alert alert, CancellationToken ct);
}

public interface IAlertService
{
    Task<Alert> CreateAlertAsync(Guid stationId, AlertType type, AlertSeverity severity, string message, CancellationToken ct);
    Task<Alert?> GetByIdAsync(Guid alertId, CancellationToken ct);
    Task ResolveAlertAsync(Guid alertId, CancellationToken ct);
    Task<List<Alert>> GetActiveAsync(Guid? stationId, Guid? branchId, CancellationToken ct);
}

public interface IPeripheralRepository
{
    Task ReplaceSnapshotAsync(Guid stationId, IReadOnlyList<Domain.Entities.Peripheral> peripherals, CancellationToken ct);
    Task<List<Domain.Entities.Peripheral>> GetAllAsync(Guid stationId, CancellationToken ct);
}

public interface IRemoteCommandRepository
{
    Task<RemoteCommand> AddAsync(RemoteCommand command, CancellationToken ct);
    Task<RemoteCommand?> GetAsync(Guid id, CancellationToken ct);
    Task<RemoteCommand?> FindPendingForAckAsync(Guid stationId, CommandType type, CancellationToken ct);
    Task UpdateAsync(RemoteCommand command, CancellationToken ct);
    Task<List<RemoteCommand>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct);
}

public interface IStationService
{
    Task<List<StationDto>> GetAllAsync(Guid? branchId, CancellationToken ct);
    Task<StationDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<RegisterStationResultDto> RegisterAsync(RegisterStationDto dto, CancellationToken ct);
    Task<bool> UpdateAsync(Guid id, UpdateStationDto dto, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    Task UpdateStatusAsync(Guid id, StationStatus status, DateTime heartbeatAt, CancellationToken ct);
}

public interface IMonitoringService
{
    Task ProcessTelemetryAsync(Guid stationId, TelemetryDto dto, CancellationToken ct);
    Task<TelemetryDto?> GetLatestTelemetryAsync(Guid stationId, CancellationToken ct);
    Task<List<TelemetryDto>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct);
    Task<StationHealthDto?> GetHealthAsync(Guid stationId, CancellationToken ct);
    Task<List<AlertDto>> GetAlertsAsync(Guid? stationId, Guid? branchId, CancellationToken ct);
    Task ProcessPeripheralsAsync(Guid stationId, IReadOnlyList<PeripheralDto> peripherals, CancellationToken ct);
    Task<List<PeripheralViewDto>> GetPeripheralsAsync(Guid stationId, CancellationToken ct);
}

public interface IRemoteControlService
{
    Task<CommandResultDto?> ExecuteAsync(Guid stationId, Domain.Enums.CommandType type, object? payload, CancellationToken ct);
    Task<CommandResultDto?> GetCommandAsync(Guid commandId, CancellationToken ct);
    Task<List<CommandResultDto>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct);
    Task HandleAcknowledgementAsync(Guid stationId, Domain.Enums.CommandType type, bool success, string? detail, Guid? commandId, CancellationToken ct);
}
