namespace NinetyBackend.Modules.Stations.DTOs;

public record AgentResponseDto(
    Guid Id,
    Guid StationId,
    string MachineName,
    string Version,
    string Status,
    DateTime? LastHeartbeatAt
);
