using System.Collections.Concurrent;

namespace NinetyBackend.Infrastructure.SignalR;

public sealed class AgentConnectionManager : IAgentConnectionManager
{
    private readonly ConcurrentDictionary<string, AgentConnection> _connections = new();

    public bool TryAdd(string connectionId, Guid agentId, Guid stationId) =>
        _connections.TryAdd(connectionId, new AgentConnection(agentId, stationId, DateTime.MinValue));

    public bool TryGet(string connectionId, out AgentConnection connection) =>
        _connections.TryGetValue(connectionId, out connection!);

    public bool TryRemove(string connectionId, out AgentConnection connection, out bool lastConnection)
    {
        if (!_connections.TryRemove(connectionId, out connection!))
        {
            lastConnection = false;
            return false;
        }

        var removedAgentId = connection.AgentId;
        lastConnection = !_connections.Values.Any(c => c.AgentId == removedAgentId);
        return true;
    }

    public bool TryAcceptTelemetry(string connectionId, TimeSpan minimumInterval)
    {
        while (_connections.TryGetValue(connectionId, out var current))
        {
            var now = DateTime.UtcNow;
            if (current.LastTelemetryAt != DateTime.MinValue && now - current.LastTelemetryAt < minimumInterval)
                return false;

            if (_connections.TryUpdate(connectionId, current with { LastTelemetryAt = now }, current))
                return true;
        }

        return false;
    }
}
