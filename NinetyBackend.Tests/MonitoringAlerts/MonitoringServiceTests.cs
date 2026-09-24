using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.MonitoringAlerts.DTOs;
using NinetyBackend.Modules.MonitoringAlerts.Models;
using NinetyBackend.Modules.MonitoringAlerts.Repositories;
using NinetyBackend.Modules.MonitoringAlerts.Services;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Stations.Repositories;
using Xunit;

namespace NinetyBackend.Tests.MonitoringAlerts;

public class MonitoringServiceTests
{
    [Fact]
    public async Task ProcessTelemetryAsync_WhenStationExists_StoresTelemetryAndCreatesAlert()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);
        var stationId = Guid.NewGuid();
        db.GamingStations.Add(new GamingStation
        {
            Id = stationId,
            Code = "ST-01",
            Name = "Test Station",
            Status = StationStatus.Available,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var telemetryRepo = new TelemetryRepository(db);
        var alertRepo = new AlertRepository(db);
        var stationRepo = new StationRepository(db);
        var service = new MonitoringService(telemetryRepo, alertRepo, stationRepo);

        await service.ProcessTelemetryAsync(stationId, new TelemetryDto(
            stationId,
            99.5,
            98.0,
            50.0,
            95.0,
            96.0,
            10,
            "Connected",
            DateTime.UtcNow), CancellationToken.None);

        var latest = await service.GetLatestTelemetryAsync(stationId, CancellationToken.None);
        var alerts = await service.GetAlertsAsync(stationId, CancellationToken.None);

        Assert.NotNull(latest);
        Assert.NotEmpty(alerts);
    }
}
