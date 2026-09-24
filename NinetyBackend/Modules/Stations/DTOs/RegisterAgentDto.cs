namespace NinetyBackend.Modules.Stations.DTOs;

public record RegisterAgentDto(
    Guid StationId,
    string MachineName,
    string Version
);
