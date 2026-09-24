namespace NinetyBackend.Modules.Stations.DTOs;

public record StationResponseDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    DateTime? LastSeenAt
);
