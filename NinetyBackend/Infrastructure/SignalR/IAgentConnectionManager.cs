namespace NinetyBackend.Infrastructure.SignalR;

public sealed record AgentConnection(Guid AgentId, Guid StationId, DateTime LastTelemetryAt);

public interface IAgentConnectionManager
{
    bool TryAdd(string connectionId, Guid agentId, Guid stationId);
    bool TryGet(string connectionId, out AgentConnection connection);
    bool TryRemove(string connectionId, out AgentConnection connection, out bool lastConnection);
    bool TryAcceptTelemetry(string connectionId, TimeSpan minimumInterval);
}
