namespace NinetyBackend.Modules.Stations.DTOs;

public record StationResponseDto(
    Guid Id,
    Guid? BranchId,
    string Code,
    string Name,
    string Status,
    DateTime? LastSeenAt
);
