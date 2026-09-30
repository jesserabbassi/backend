using NinetyBackend.Modules.Customers.Models;
using NinetyBackend.Modules.Customers.Repositories;
using NinetyBackend.Modules.Reservations.DTOs;
using NinetyBackend.Modules.Reservations.Models;
using NinetyBackend.Modules.Reservations.Repositories;
using NinetyBackend.Modules.Stations.Repositories;

namespace NinetyBackend.Modules.Reservations.Services;

public class ReservationService(
    IReservationRepository reservationRepository,
    ICustomerRepository customerRepository,
    IStationRepository stationRepository,
    IAvailabilityService availabilityService) : IReservationService
{
    public async Task<ReservationResponseDto> CreateAsync(CreateReservationDto request)
    {
        ValidateWindow(request.BranchId, request.StartTime, request.EndTime);
        if (request.CustomerId == Guid.Empty || request.StationId == Guid.Empty)
        {
            throw new InvalidOperationException("Customer and station are required.");
        }

        var customer = await customerRepository.GetByIdAsync(request.CustomerId);
        if (customer == null || customer.Status != CustomerStatus.ACTIVE)
        {
            throw new InvalidOperationException("An active customer is required.");
        }

        var station = await stationRepository.GetByIdAsync(request.StationId);
        if (station == null || station.BranchId != request.BranchId)
        {
            throw new InvalidOperationException("Station was not found in the specified branch.");
        }

        if (!await availabilityService.IsAvailableAsync(
                request.BranchId, request.StationId, request.StartTime, request.EndTime))
        {
            throw new InvalidOperationException("Station is not available for the requested time.");
        }

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            BranchId = request.BranchId,
            StationId = request.StationId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = ReservationStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };

        await reservationRepository.CreateAsync(reservation);
        return MapToResponseDto(reservation);
    }

    public async Task<ReservationResponseDto?> GetByIdAsync(Guid id)
    {
        var reservation = await reservationRepository.GetByIdAsync(id);
        return reservation == null ? null : MapToResponseDto(reservation);
    }

    public async Task<IEnumerable<ReservationResponseDto>> GetByCustomerAsync(Guid customerId)
    {
        var reservations = await reservationRepository.GetByCustomerAsync(customerId);
        return reservations.Select(MapToResponseDto);
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var reservation = await reservationRepository.GetByIdAsync(id);
        if (reservation == null)
        {
            return false;
        }

        if (reservation.Status is ReservationStatus.COMPLETED or ReservationStatus.CANCELLED or ReservationStatus.EXPIRED)
        {
            throw new InvalidOperationException("This reservation can no longer be cancelled.");
        }

        reservation.Status = ReservationStatus.CANCELLED;
        await reservationRepository.UpdateAsync(reservation);
        return true;
    }

    public async Task<ReservationResponseDto?> ConfirmAsync(Guid id)
    {
        var reservation = await reservationRepository.GetByIdAsync(id);
        if (reservation == null)
        {
            return null;
        }

        if (reservation.Status != ReservationStatus.PENDING)
        {
            throw new InvalidOperationException("Only pending reservations can be confirmed.");
        }

        if (!await availabilityService.IsAvailableAsync(
                reservation.BranchId,
                reservation.StationId,
                reservation.StartTime,
                reservation.EndTime,
                reservation.Id))
        {
            throw new InvalidOperationException("Station is no longer available for this reservation.");
        }

        reservation.Status = ReservationStatus.CONFIRMED;
        await reservationRepository.UpdateAsync(reservation);
        return MapToResponseDto(reservation);
    }

    public Task<IEnumerable<NinetyBackend.Modules.Stations.DTOs.StationResponseDto>> CheckAvailabilityAsync(
        AvailabilityRequestDto request)
    {
        ValidateWindow(request.BranchId, request.StartTime, request.EndTime);
        return availabilityService.FindAvailableStationsAsync(request);
    }

    private static void ValidateWindow(Guid branchId, DateTime startTime, DateTime endTime)
    {
        if (branchId == Guid.Empty)
        {
            throw new InvalidOperationException("Branch is required.");
        }

        if (startTime.Kind != DateTimeKind.Utc || endTime.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException("Reservation times must be provided in UTC.");
        }

        if (startTime <= DateTime.UtcNow || endTime <= startTime)
        {
            throw new InvalidOperationException("Reservation time must be in the future and end after it starts.");
        }
    }

    private static ReservationResponseDto MapToResponseDto(Reservation reservation)
    {
        return new ReservationResponseDto(
            reservation.Id,
            reservation.CustomerId,
            reservation.BranchId,
            reservation.StationId,
            reservation.StartTime,
            reservation.EndTime,
            reservation.Status.ToString(),
            reservation.CreatedAt);
    }
}