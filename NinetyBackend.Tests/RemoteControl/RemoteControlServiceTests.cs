using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.RemoteControl.Models;
using NinetyBackend.Modules.RemoteControl.Repositories;
using NinetyBackend.Modules.RemoteControl.Services;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Stations.Repositories;
using Xunit;

namespace NinetyBackend.Tests.RemoteControl;

public class RemoteControlServiceTests
{
    [Fact]
    public async Task SendCommandAsync_WhenStationExists_StoresPendingCommand()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);

        var stationId = Guid.NewGuid();
        db.GamingStations.Add(new GamingStation
        {
            Id = stationId,
            Code = "ST-REMOTE",
            Name = "Remote Test Station",
            Status = StationStatus.Available,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var repo = new CommandRepository(db);
        var connectionService = new InMemoryAgentConnectionService();
        var stationRepository = new StationRepository(db);
        var service = new RemoteControlService(repo, connectionService, stationRepository);

        connectionService.MarkConnected(stationId);

        var result = await service.SendCommandAsync(stationId, CommandType.Lock, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(stationId, result.StationId);
        Assert.Equal(CommandType.Lock, result.Type);
        Assert.Equal(CommandStatus.Sent, result.Status);
    }
}
