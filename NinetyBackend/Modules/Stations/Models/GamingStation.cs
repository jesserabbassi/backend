namespace NinetyBackend.Modules.Stations.Models;

public class GamingStation
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public StationStatus Status { get; set; } = StationStatus.Offline;
    public Guid? AgentId { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum StationStatus
{
    Available,
    InUse,
    Offline,
    Maintenance
}
