using NinetyBackend.Infrastructure.SignalR;

namespace NinetyBackend.Tests.Stations;

public class AgentConnectionManagerTests
{
    [Fact]
    public void RemovingOneOfTwoConnections_DoesNotMarkAgentAsLast()
    {
        var manager = new AgentConnectionManager();
        var agentId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        manager.TryAdd("first", agentId, stationId);
        manager.TryAdd("second", agentId, stationId);

        var removed = manager.TryRemove("first", out _, out var lastConnection);

        Assert.True(removed);
        Assert.False(lastConnection);
    }

    [Fact]
    public void RemovingLastConnection_MarksAgentAsLast()
    {
        var manager = new AgentConnectionManager();
        manager.TryAdd("only", Guid.NewGuid(), Guid.NewGuid());

        var removed = manager.TryRemove("only", out _, out var lastConnection);

        Assert.True(removed);
        Assert.True(lastConnection);
    }

    [Fact]
    public void TelemetryIsRateLimitedPerConnection()
    {
        var manager = new AgentConnectionManager();
        manager.TryAdd("agent", Guid.NewGuid(), Guid.NewGuid());

        Assert.True(manager.TryAcceptTelemetry("agent", TimeSpan.FromSeconds(1)));
        Assert.False(manager.TryAcceptTelemetry("agent", TimeSpan.FromSeconds(1)));
    }
}
