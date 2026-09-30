namespace NinetyBackend.Modules.Reservations.DTOs;

public record ReservationResponseDto(
    Guid Id,
    Guid CustomerId,
    Guid BranchId,
    Guid StationId,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    DateTime CreatedAt
);