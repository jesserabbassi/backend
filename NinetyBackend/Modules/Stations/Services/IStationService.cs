using NinetyBackend.Modules.Stations.DTOs;

namespace NinetyBackend.Modules.Stations.Services;

public interface IStationService
{
    Task<IEnumerable<StationResponseDto>> GetAllAsync();
    Task<StationResponseDto?> GetByIdAsync(Guid id);
    Task<StationResponseDto> CreateAsync(CreateStationDto dto);
    Task<StationResponseDto?> UpdateAsync(Guid id, UpdateStationDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<string?> GetStatusAsync(Guid id);
}
