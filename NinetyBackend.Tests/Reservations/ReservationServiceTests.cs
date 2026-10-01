using Moq;
using NinetyBackend.Modules.Customers.Models;
using NinetyBackend.Modules.Customers.Repositories;
using NinetyBackend.Modules.Reservations.DTOs;
using NinetyBackend.Modules.Reservations.Models;
using NinetyBackend.Modules.Reservations.Repositories;
using NinetyBackend.Modules.Reservations.Services;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Stations.Repositories;

namespace NinetyBackend.Tests.Reservations;

public class ReservationServiceTests
{
    private readonly Mock<IReservationRepository> _reservationRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IAvailabilityService> _availabilityService = new();
    private readonly ReservationService _service;

    public ReservationServiceTests()
    {
        _service = new ReservationService(
            _reservationRepository.Object,
            _customerRepository.Object,
            _stationRepository.Object,
            _availabilityService.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesPendingReservation()
    {
        var request = ValidRequest();
        _customerRepository.Setup(repository => repository.GetByIdAsync(request.CustomerId))
            .ReturnsAsync(new Customer { Id = request.CustomerId, Status = CustomerStatus.ACTIVE });
        _stationRepository.Setup(repository => repository.GetByIdAsync(request.StationId))
            .ReturnsAsync(new GamingStation
            {
                Id = request.StationId,
                BranchId = request.BranchId,
                Status = StationStatus.Available
            });
        _availabilityService.Setup(service => service.IsAvailableAsync(
                request.BranchId, request.StationId, request.StartTime, request.EndTime, null))
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(request);

        Assert.Equal(request.CustomerId, result.CustomerId);
        Assert.Equal("PENDING", result.Status);
        _reservationRepository.Verify(repository => repository.CreateAsync(It.Is<Reservation>(reservation =>
            reservation.BranchId == request.BranchId &&
            reservation.StationId == request.StationId &&
            reservation.Status == ReservationStatus.PENDING)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenStationIsUnavailable_ThrowsAndDoesNotCreate()
    {
        var request = ValidRequest();
        _customerRepository.Setup(repository => repository.GetByIdAsync(request.CustomerId))
            .ReturnsAsync(new Customer { Id = request.CustomerId, Status = CustomerStatus.ACTIVE });
        _stationRepository.Setup(repository => repository.GetByIdAsync(request.StationId))
            .ReturnsAsync(new GamingStation { Id = request.StationId, BranchId = request.BranchId });
        _availabilityService.Setup(service => service.IsAvailableAsync(
                request.BranchId, request.StationId, request.StartTime, request.EndTime, null))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
        _reservationRepository.Verify(repository => repository.CreateAsync(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenEndIsBeforeStart_Throws()
    {
        var request = ValidRequest() with { EndTime = DateTime.UtcNow.AddMinutes(30) };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CancelAsync_WhenReservationExists_SetsCancelledStatus()
    {
        var reservation = new Reservation { Id = Guid.NewGuid(), Status = ReservationStatus.CONFIRMED };
        _reservationRepository.Setup(repository => repository.GetByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        var result = await _service.CancelAsync(reservation.Id);

        Assert.True(result);
        Assert.Equal(ReservationStatus.CANCELLED, reservation.Status);
        _reservationRepository.Verify(repository => repository.UpdateAsync(reservation), Times.Once);
    }

    private static CreateReservationDto ValidRequest()
    {
        var start = DateTime.UtcNow.AddDays(1);
        return new CreateReservationDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            start.AddHours(1));
    }
}