namespace NinetyBackend.Modules.Reservations.DTOs;

public record AvailabilityRequestDto(
    Guid BranchId,
    DateTime StartTime,
    DateTime EndTime
);