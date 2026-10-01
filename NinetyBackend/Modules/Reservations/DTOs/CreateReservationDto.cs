namespace NinetyBackend.Modules.Reservations.DTOs;

public record CreateReservationDto(
    Guid CustomerId,
    Guid BranchId,
    Guid StationId,
    DateTime StartTime,
    DateTime EndTime
);