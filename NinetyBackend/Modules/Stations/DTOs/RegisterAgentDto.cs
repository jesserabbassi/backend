namespace NinetyBackend.Modules.Stations.DTOs;

public record RegisterAgentDto(
    Guid StationId,
    string MachineName,
    string Version
);

public record AgentRegistrationResponseDto(Guid AgentId, Guid StationId, string Status, DateTime RegisteredAt, string BootstrapSecret, string? MachineName = null, string? Version = null, DateTime? LastHeartbeatAt = null);
public record AgentBootstrapDto(string BootstrapSecret);
