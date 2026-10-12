namespace NinetyBackend.Modules.Stations.Models;

public class AgentRefreshToken
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }

    public Agent Agent { get; set; } = null!;

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
}
