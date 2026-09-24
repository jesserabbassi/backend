using Moq;
using NinetyBackend.Modules.Stations.DTOs;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Stations.Repositories;
using NinetyBackend.Modules.Stations.Services;
using Xunit;

namespace NinetyBackend.Tests.Stations;

public class StationServiceTests
{
    private readonly Mock<IStationRepository> _stationRepo = new();
    private readonly StationService _service;

    public StationServiceTests()
    {
        _service = new StationService(_stationRepo.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllMappedStations()
    {
        var stations = new List<GamingStation>
        {
            new() { Id = Guid.NewGuid(), Code = "ST-01", Name = "Station 1", Status = StationStatus.Available },
            new() { Id = Guid.NewGuid(), Code = "ST-02", Name = "Station 2", Status = StationStatus.Offline }
        };
        _stationRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(stations);

        var result = (await _service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("ST-01", result[0].Code);
        Assert.Equal("Available", result[0].Status);
        Assert.Equal("ST-02", result[1].Code);
        Assert.Equal("Offline", result[1].Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenStationExists_ReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var station = new GamingStation { Id = id, Code = "ST-01", Name = "Station 1", Status = StationStatus.Available };
        _stationRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(station);

        var result = await _service.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("ST-01", result.Code);
        Assert.Equal("Station 1", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _stationRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((GamingStation?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesAndReturnsStation()
    {
        _stationRepo.Setup(r => r.AddAsync(It.IsAny<GamingStation>())).Returns(Task.CompletedTask);

        var dto = new CreateStationDto("ST-01", "Station 1");
        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("ST-01", result.Code);
        Assert.Equal("Station 1", result.Name);
        Assert.Equal("Offline", result.Status);
        _stationRepo.Verify(r => r.AddAsync(It.Is<GamingStation>(s => s.Code == "ST-01" && s.Name == "Station 1")), Times.Once);
    }

    [Theory]
    [InlineData("", "Station 1")]
    [InlineData(" ", "Station 1")]
    [InlineData(null, "Station 1")]
    public async Task CreateAsync_WithInvalidCode_ThrowsInvalidOperationException(string? code, string name)
    {
        var dto = new CreateStationDto(code!, name);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Theory]
    [InlineData("ST-01", "")]
    [InlineData("ST-01", " ")]
    [InlineData("ST-01", null)]
    public async Task CreateAsync_WithInvalidName_ThrowsInvalidOperationException(string code, string? name)
    {
        var dto = new CreateStationDto(code, name!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenStationExists_UpdatesAndReturnsDto()
    {
        var id = Guid.NewGuid();
        var station = new GamingStation { Id = id, Code = "ST-01", Name = "Old Name", Status = StationStatus.Available };
        _stationRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(station);
        _stationRepo.Setup(r => r.UpdateAsync(station)).Returns(Task.CompletedTask);

        var dto = new UpdateStationDto("New Name");
        var result = await _service.UpdateAsync(id, dto);

        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("New Name", station.Name);
        _stationRepo.Verify(r => r.UpdateAsync(station), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        _stationRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((GamingStation?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateStationDto("New Name"));

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WhenStationExists_DeletesAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        var station = new GamingStation { Id = id, Code = "ST-01", Name = "Station 1" };
        _stationRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(station);
        _stationRepo.Setup(r => r.DeleteAsync(station)).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(id);

        Assert.True(result);
        _stationRepo.Verify(r => r.DeleteAsync(station), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        _stationRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((GamingStation?)null);

        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task GetStatusAsync_WhenStationExists_ReturnsStatusString()
    {
        var id = Guid.NewGuid();
        var station = new GamingStation { Id = id, Code = "ST-01", Name = "Station 1", Status = StationStatus.InUse };
        _stationRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(station);

        var result = await _service.GetStatusAsync(id);

        Assert.Equal("InUse", result);
    }

    [Fact]
    public async Task GetStatusAsync_WhenNotFound_ReturnsNull()
    {
        _stationRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((GamingStation?)null);

        var result = await _service.GetStatusAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
