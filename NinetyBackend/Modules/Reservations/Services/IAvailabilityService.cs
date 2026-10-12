using NinetyBackend.Modules.Reservations.DTOs;
using NinetyBackend.Modules.Stations.DTOs;

namespace NinetyBackend.Modules.Reservations.Services;

public interface IAvailabilityService
{
    Task<bool> IsAvailableAsync(
        Guid branchId,
        Guid stationId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludingReservationId = null);
    Task<IEnumerable<StationResponseDto>> FindAvailableStationsAsync(AvailabilityRequestDto request);
}