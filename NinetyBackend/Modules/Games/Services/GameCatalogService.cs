using NinetyBackend.Modules.Games.DTOs;
using NinetyBackend.Modules.Games.Models;
using NinetyBackend.Modules.Games.Repositories;

namespace NinetyBackend.Modules.Games.Services;

public class GameCatalogService(IGameRepository gameRepository) : IGameCatalogService
{
    public async Task<IEnumerable<GameResponseDto>> GetAllAsync()
    {
        var games = await gameRepository.GetAllAsync();
        return games.Select(MapToResponseDto);
    }

    public async Task<GameResponseDto?> GetByIdAsync(Guid id)
    {
        var game = await gameRepository.GetByIdAsync(id);
        return game is null ? null : MapToResponseDto(game);
    }

    public async Task<GameResponseDto> CreateAsync(CreateGameDto dto)
    {
        ValidateRequiredFields(dto.Name, dto.Publisher, dto.Version, dto.Description);

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Publisher = dto.Publisher.Trim(),
            Version = dto.Version.Trim(),
            Description = dto.Description.Trim(),
            Status = GameStatus.ACTIVE
        };

        await gameRepository.AddAsync(game);
        return MapToResponseDto(game);
    }

    public async Task<GameResponseDto?> UpdateAsync(Guid id, UpdateGameDto dto)
    {
        var game = await gameRepository.GetByIdAsync(id);
        if (game is null)
        {
            return null;
        }

        ValidateRequiredFields(dto.Name, dto.Publisher, dto.Version, dto.Description);

        game.Name = dto.Name.Trim();
        game.Publisher = dto.Publisher.Trim();
        game.Version = dto.Version.Trim();
        game.Description = dto.Description.Trim();

        await gameRepository.UpdateAsync(game);
        return MapToResponseDto(game);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var game = await gameRepository.GetByIdAsync(id);
        if (game is null)
        {
            return false;
        }

        game.Status = GameStatus.INACTIVE;
        await gameRepository.DeleteAsync(game);
        return true;
    }

    private static void ValidateRequiredFields(params string?[] fields)
    {
        if (fields.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Name, publisher, version, and description are required.");
        }
    }

    private static GameResponseDto MapToResponseDto(Game game)
    {
        return new GameResponseDto(
            game.Id,
            game.Name,
            game.Publisher,
            game.Version,
            game.Description,
            game.Status);
    }
}