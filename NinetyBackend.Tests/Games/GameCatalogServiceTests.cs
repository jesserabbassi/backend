using Moq;
using NinetyBackend.Modules.Games.DTOs;
using NinetyBackend.Modules.Games.Models;
using NinetyBackend.Modules.Games.Repositories;
using NinetyBackend.Modules.Games.Services;

namespace NinetyBackend.Tests.Games;

public class GameCatalogServiceTests
{
    private readonly Mock<IGameRepository> _gameRepository = new();
    private readonly GameCatalogService _service;

    public GameCatalogServiceTests()
    {
        _service = new GameCatalogService(_gameRepository.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsGamesMappedToDtos()
    {
        var games = new List<Game>
        {
            new() { Id = Guid.NewGuid(), Name = "Game A", Publisher = "Publisher A", Version = "1.0", Description = "First" },
            new() { Id = Guid.NewGuid(), Name = "Game B", Publisher = "Publisher B", Version = "2.0", Description = "Second" }
        };
        _gameRepository.Setup(repository => repository.GetAllAsync()).ReturnsAsync(games);

        var result = (await _service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(games[0].Id, result[0].Id);
        Assert.Equal(GameStatus.ACTIVE, result[0].Status);
        Assert.Equal("Game B", result[1].Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        _gameRepository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ValidatesTrimsAndCreatesActiveGame()
    {
        var dto = new CreateGameDto("  Game  ", " Publisher ", " 1.0 ", " Description ");

        var result = await _service.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Game", result.Name);
        Assert.Equal("Publisher", result.Publisher);
        Assert.Equal("1.0", result.Version);
        Assert.Equal("Description", result.Description);
        Assert.Equal(GameStatus.ACTIVE, result.Status);
        _gameRepository.Verify(repository => repository.AddAsync(It.Is<Game>(game => game.Id == result.Id)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenGameExistsUpdatesFieldsAndKeepsStatus()
    {
        var id = Guid.NewGuid();
        var game = new Game { Id = id, Name = "Old", Publisher = "Old Pub", Version = "1", Description = "Old description", Status = GameStatus.INACTIVE };
        _gameRepository.Setup(repository => repository.GetByIdAsync(id)).ReturnsAsync(game);

        var result = await _service.UpdateAsync(id, new UpdateGameDto("New", "New Pub", "2", "New description"));

        Assert.NotNull(result);
        Assert.Equal("New", result.Name);
        Assert.Equal(GameStatus.INACTIVE, result.Status);
        _gameRepository.Verify(repository => repository.UpdateAsync(game), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenGameExistsMarksInactive()
    {
        var id = Guid.NewGuid();
        var game = new Game { Id = id, Name = "Game", Publisher = "Publisher", Version = "1", Description = "Description" };
        _gameRepository.Setup(repository => repository.GetByIdAsync(id)).ReturnsAsync(game);

        var result = await _service.DeleteAsync(id);

        Assert.True(result);
        Assert.Equal(GameStatus.INACTIVE, game.Status);
        _gameRepository.Verify(repository => repository.DeleteAsync(game), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenGameIsMissingReturnsFalse()
    {
        _gameRepository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);

        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
        _gameRepository.Verify(repository => repository.DeleteAsync(It.IsAny<Game>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenRequiredFieldIsBlankThrows()
    {
        var dto = new CreateGameDto(" ", "Publisher", "1.0", "Description");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
        _gameRepository.Verify(repository => repository.AddAsync(It.IsAny<Game>()), Times.Never);
    }
}