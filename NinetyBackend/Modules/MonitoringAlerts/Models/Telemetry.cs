namespace NinetyBackend.Modules.MonitoringAlerts.Models;

public class Telemetry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StationId { get; set; }
    public double CpuUsage { get; set; }
    public double GpuUsage { get; set; }
    public double RamUsage { get; set; }
    public double CpuTemperature { get; set; }
    public double GpuTemperature { get; set; }
    public double FanSpeed { get; set; }
    public string? NetworkStatus { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum AlertType
{
    HighCpu,
    HighGpu,
    HighTemperature,
    FanFailure,
    UsbChange,
    StationOffline
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public class Alert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StationId { get; set; }
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
