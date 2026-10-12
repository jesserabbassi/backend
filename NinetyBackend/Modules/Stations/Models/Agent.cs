namespace NinetyBackend.Modules.Stations.Models;

public class Agent
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public string MachineName { get; set; } = null!;
    public string Version { get; set; } = null!;
    public AgentStatus Status { get; set; } = AgentStatus.Offline;
    public DateTime? LastHeartbeatAt { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? BootstrapSecretHash { get; set; }
}

public enum AgentStatus
{
    Pending,
    Approved,
    Rejected,
    Revoked,
    Online,
    Offline,
    Connecting
}
