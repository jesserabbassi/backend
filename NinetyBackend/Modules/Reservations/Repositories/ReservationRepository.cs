using Microsoft.EntityFrameworkCore;
using Npgsql;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.Reservations.Models;

namespace NinetyBackend.Modules.Reservations.Repositories;

public class ReservationRepository(ApplicationDbContext db) : IReservationRepository
{
    public Task<Reservation?> GetByIdAsync(Guid id)
    {
        return db.Reservations.FirstOrDefaultAsync(reservation => reservation.Id == id);
    }

    public async Task<IEnumerable<Reservation>> GetByCustomerAsync(Guid customerId)
    {
        return await db.Reservations
            .Where(reservation => reservation.CustomerId == customerId)
            .OrderByDescending(reservation => reservation.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetOverlappingAsync(
        Guid branchId,
        Guid stationId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludingReservationId = null)
    {
        return await db.Reservations
            .Where(reservation =>
                reservation.BranchId == branchId &&
                reservation.StationId == stationId &&
                reservation.StartTime < endTime &&
                reservation.EndTime > startTime &&
                reservation.Status != ReservationStatus.CANCELLED &&
                reservation.Status != ReservationStatus.COMPLETED &&
                reservation.Status != ReservationStatus.EXPIRED &&
                (!excludingReservationId.HasValue || reservation.Id != excludingReservationId.Value))
            .ToListAsync();
    }

    public async Task CreateAsync(Reservation reservation)
    {
        db.Reservations.Add(reservation);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsReservationOverlap(ex))
        {
            throw new InvalidOperationException("Station is not available for the requested time.", ex);
        }
    }

    public async Task UpdateAsync(Reservation reservation)
    {
        db.Reservations.Update(reservation);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsReservationOverlap(ex))
        {
            throw new InvalidOperationException("Station is not available for the requested time.", ex);
        }
    }

    private static bool IsReservationOverlap(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ExclusionViolation,
            ConstraintName: "EX_Reservations_Station_Time"
        };
    }
}