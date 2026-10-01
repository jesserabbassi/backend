namespace NinetyBackend.Modules.Stations.DTOs;

public record CreateStationDto(
    string Code,
    string Name,
    Guid? BranchId = null
);
