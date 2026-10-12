using NinetyBackend.Modules.Games.DTOs;

namespace NinetyBackend.Modules.Games.Services;

public interface IGameCatalogService
{
    Task<IEnumerable<GameResponseDto>> GetAllAsync();
    Task<GameResponseDto?> GetByIdAsync(Guid id);
    Task<GameResponseDto> CreateAsync(CreateGameDto dto);
    Task<GameResponseDto?> UpdateAsync(Guid id, UpdateGameDto dto);
    Task<bool> DeleteAsync(Guid id);
}