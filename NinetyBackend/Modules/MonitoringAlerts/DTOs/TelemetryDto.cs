namespace NinetyBackend.Modules.MonitoringAlerts.DTOs;

public record TelemetryDto(
    Guid StationId,
    double CpuUsage,
    double GpuUsage,
    double RamUsage,
    double CpuTemperature,
    double GpuTemperature,
    double FanSpeed,
    string? NetworkStatus,
    DateTime Timestamp
);

public record AlertDto(
    Guid Id,
    Guid StationId,
    string Type,
    string Severity,
    string Message,
    DateTime CreatedAt,
    DateTime? ResolvedAt
);
