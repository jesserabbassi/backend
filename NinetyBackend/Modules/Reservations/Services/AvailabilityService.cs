using NinetyBackend.Modules.Reservations.DTOs;
using NinetyBackend.Modules.Reservations.Repositories;
using NinetyBackend.Modules.Stations.DTOs;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Stations.Repositories;

namespace NinetyBackend.Modules.Reservations.Services;

public class AvailabilityService(
    IReservationRepository reservationRepository,
    IStationRepository stationRepository) : IAvailabilityService
{
    public async Task<bool> IsAvailableAsync(
        Guid branchId,
        Guid stationId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludingReservationId = null)
    {
        var station = await stationRepository.GetByIdAsync(stationId);
        if (station == null || station.BranchId != branchId || station.Status != StationStatus.Available)
        {
            return false;
        }

        var overlaps = await reservationRepository.GetOverlappingAsync(
            branchId,
            stationId,
            startTime,
            endTime,
            excludingReservationId);
        return !overlaps.Any();
    }

    public async Task<IEnumerable<StationResponseDto>> FindAvailableStationsAsync(AvailabilityRequestDto request)
    {
        var stations = await stationRepository.GetAllAsync();
        var availableStations = new List<StationResponseDto>();

        foreach (var station in stations.Where(station =>
                     station.BranchId == request.BranchId && station.Status == StationStatus.Available))
        {
            if (await IsAvailableAsync(request.BranchId, station.Id, request.StartTime, request.EndTime))
            {
                availableStations.Add(MapToResponseDto(station));
            }
        }

        return availableStations;
    }

    private static StationResponseDto MapToResponseDto(GamingStation station)
    {
        return new StationResponseDto(
            station.Id,
            station.BranchId,
            station.Code,
            station.Name,
            station.Status.ToString(),
            station.LastSeenAt);
    }
}