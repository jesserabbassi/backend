using NinetyBackend.Modules.Reservations.DTOs;

namespace NinetyBackend.Modules.Reservations.Services;

public interface IReservationService
{
    Task<ReservationResponseDto> CreateAsync(CreateReservationDto request);
    Task<ReservationResponseDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ReservationResponseDto>> GetByCustomerAsync(Guid customerId);
    Task<bool> CancelAsync(Guid id);
    Task<ReservationResponseDto?> ConfirmAsync(Guid id);
    Task<IEnumerable<NinetyBackend.Modules.Stations.DTOs.StationResponseDto>> CheckAvailabilityAsync(
        AvailabilityRequestDto request);
}