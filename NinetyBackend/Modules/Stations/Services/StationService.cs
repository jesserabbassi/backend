using NinetyBackend.Modules.Stations.DTOs;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Stations.Repositories;

namespace NinetyBackend.Modules.Stations.Services;

public class StationService : IStationService
{
    private readonly IStationRepository _stationRepository;

    public StationService(IStationRepository stationRepository)
    {
        _stationRepository = stationRepository;
    }

    public async Task<IEnumerable<StationResponseDto>> GetAllAsync()
    {
        var stations = await _stationRepository.GetAllAsync();
        return stations.Select(MapToResponseDto);
    }

    public async Task<StationResponseDto?> GetByIdAsync(Guid id)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        return station == null ? null : MapToResponseDto(station);
    }

    public async Task<StationResponseDto> CreateAsync(CreateStationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            throw new InvalidOperationException("Station code is required.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Station name is required.");

        var station = new GamingStation
        {
            Id = Guid.NewGuid(),
            BranchId = dto.BranchId,
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            Status = StationStatus.Offline,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _stationRepository.AddAsync(station);
        return MapToResponseDto(station);
    }

    public async Task<StationResponseDto?> UpdateAsync(Guid id, UpdateStationDto dto)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null) return null;

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Station name cannot be empty.");

        station.Name = dto.Name.Trim();
        station.UpdatedAt = DateTime.UtcNow;

        await _stationRepository.UpdateAsync(station);
        return MapToResponseDto(station);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null) return false;

        await _stationRepository.DeleteAsync(station);
        return true;
    }

    public async Task<string?> GetStatusAsync(Guid id)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        return station?.Status.ToString();
    }

    private static StationResponseDto MapToResponseDto(GamingStation station)
    {
        return new StationResponseDto(
            station.Id,
            station.BranchId,
            station.Code,
            station.Name,
            station.Status.ToString(),
            station.LastSeenAt
        );
    }
}
