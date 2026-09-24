using Moq;
using NinetyBackend.Modules.Stations.DTOs;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Stations.Repositories;
using NinetyBackend.Modules.Stations.Services;
using Xunit;

namespace NinetyBackend.Tests.Stations;

public class AgentServiceTests
{
    private readonly Mock<IAgentRepository> _agentRepo = new();
    private readonly Mock<IStationRepository> _stationRepo = new();
    private readonly AgentService _service;

    public AgentServiceTests()
    {
        _service = new AgentService(_agentRepo.Object, _stationRepo.Object);
    }

    [Fact]
    public async Task RegisterAsync_WhenStationNotFound_ThrowsInvalidOperationException()
    {
        var stationId = Guid.NewGuid();
        _stationRepo.Setup(r => r.GetByIdAsync(stationId)).ReturnsAsync((GamingStation?)null);

        var dto = new RegisterAgentDto(stationId, "PC-01", "1.0.0");
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegisterAsync(dto));
    }

    [Fact]
    public async Task RegisterAsync_WhenNewAgent_CreatesAgentAndUpdatesStation()
    {
        var stationId = Guid.NewGuid();
        var station = new GamingStation { Id = stationId, Code = "ST-01", Name = "Station 1", Status = StationStatus.Offline };

        _stationRepo.Setup(r => r.GetByIdAsync(stationId)).ReturnsAsync(station);
        _agentRepo.Setup(r => r.GetByStationIdAsync(stationId)).ReturnsAsync((Agent?)null);
        _agentRepo.Setup(r => r.AddAsync(It.IsAny<Agent>())).Returns(Task.CompletedTask);
        _stationRepo.Setup(r => r.UpdateAsync(station)).Returns(Task.CompletedTask);

        var dto = new RegisterAgentDto(stationId, "PC-01", "1.0.0");
        var result = await _service.RegisterAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(stationId, result.StationId);
        Assert.Equal("PC-01", result.MachineName);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal("Online", result.Status);
        Assert.NotNull(result.LastHeartbeatAt);

        Assert.Equal(StationStatus.Available, station.Status);
        Assert.NotNull(station.AgentId);
        _agentRepo.Verify(r => r.AddAsync(It.Is<Agent>(a => a.StationId == stationId && a.MachineName == "PC-01")), Times.Once);
        _stationRepo.Verify(r => r.UpdateAsync(station), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenExistingAgent_UpdatesAgentAndStation()
    {
        var stationId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var station = new GamingStation { Id = stationId, Code = "ST-01", Name = "Station 1", Status = StationStatus.Offline };
        var existingAgent = new Agent { Id = agentId, StationId = stationId, MachineName = "OLD-PC", Version = "0.9.0", Status = AgentStatus.Offline };

        _stationRepo.Setup(r => r.GetByIdAsync(stationId)).ReturnsAsync(station);
        _agentRepo.Setup(r => r.GetByStationIdAsync(stationId)).ReturnsAsync(existingAgent);
        _agentRepo.Setup(r => r.UpdateAsync(existingAgent)).Returns(Task.CompletedTask);
        _stationRepo.Setup(r => r.UpdateAsync(station)).Returns(Task.CompletedTask);

        var dto = new RegisterAgentDto(stationId, "NEW-PC", "1.1.0");
        var result = await _service.RegisterAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("NEW-PC", existingAgent.MachineName);
        Assert.Equal("1.1.0", existingAgent.Version);
        Assert.Equal(AgentStatus.Online, existingAgent.Status);
        _agentRepo.Verify(r => r.UpdateAsync(existingAgent), Times.Once);
    }

    [Fact]
    public async Task HeartbeatAsync_WhenAgentExists_UpdatesHeartbeatAndStation()
    {
        var agentId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var agent = new Agent { Id = agentId, StationId = stationId, MachineName = "PC-01", Version = "1.0.0", Status = AgentStatus.Online };
        var station = new GamingStation { Id = stationId, Code = "ST-01", Name = "Station 1", Status = StationStatus.Offline };

        _agentRepo.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);
        _stationRepo.Setup(r => r.GetByIdAsync(stationId)).ReturnsAsync(station);
        _agentRepo.Setup(r => r.UpdateAsync(agent)).Returns(Task.CompletedTask);
        _stationRepo.Setup(r => r.UpdateAsync(station)).Returns(Task.CompletedTask);

        var result = await _service.HeartbeatAsync(agentId);

        Assert.NotNull(result);
        Assert.NotNull(agent.LastHeartbeatAt);
        Assert.Equal(AgentStatus.Online, agent.Status);
        Assert.Equal(StationStatus.Available, station.Status);
        _agentRepo.Verify(r => r.UpdateAsync(agent), Times.Once);
        _stationRepo.Verify(r => r.UpdateAsync(station), Times.Once);
    }

    [Fact]
    public async Task HeartbeatAsync_WhenAgentNotFound_ReturnsNull()
    {
        _agentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Agent?)null);

        var result = await _service.HeartbeatAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task DisconnectAsync_WhenAgentExists_SetsOfflineAndUpdatesStation()
    {
        var agentId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var agent = new Agent { Id = agentId, StationId = stationId, MachineName = "PC-01", Version = "1.0.0", Status = AgentStatus.Online };
        var station = new GamingStation { Id = stationId, Code = "ST-01", Name = "Station 1", Status = StationStatus.Available };

        _agentRepo.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);
        _stationRepo.Setup(r => r.GetByIdAsync(stationId)).ReturnsAsync(station);
        _agentRepo.Setup(r => r.UpdateAsync(agent)).Returns(Task.CompletedTask);
        _stationRepo.Setup(r => r.UpdateAsync(station)).Returns(Task.CompletedTask);

        var result = await _service.DisconnectAsync(agentId);

        Assert.True(result);
        Assert.Equal(AgentStatus.Offline, agent.Status);
        Assert.Equal(StationStatus.Offline, station.Status);
        _agentRepo.Verify(r => r.UpdateAsync(agent), Times.Once);
        _stationRepo.Verify(r => r.UpdateAsync(station), Times.Once);
    }

    [Fact]
    public async Task DisconnectAsync_WhenAgentNotFound_ReturnsFalse()
    {
        _agentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Agent?)null);

        var result = await _service.DisconnectAsync(Guid.NewGuid());

        Assert.False(result);
    }
}
