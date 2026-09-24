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
}

public enum AgentStatus
{
    Online,
    Offline,
    Connecting
}
