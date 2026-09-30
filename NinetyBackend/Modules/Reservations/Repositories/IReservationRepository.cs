using NinetyBackend.Modules.Reservations.Models;

namespace NinetyBackend.Modules.Reservations.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id);
    Task<IEnumerable<Reservation>> GetByCustomerAsync(Guid customerId);
    Task<IEnumerable<Reservation>> GetOverlappingAsync(
        Guid branchId,
        Guid stationId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludingReservationId = null);
    Task CreateAsync(Reservation reservation);
    Task UpdateAsync(Reservation reservation);
}