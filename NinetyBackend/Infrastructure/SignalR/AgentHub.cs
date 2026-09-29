using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NinetyBackend.Modules.MonitoringAlerts.DTOs;
using NinetyBackend.Modules.MonitoringAlerts.Services;
using NinetyBackend.Modules.Stations.Services;

namespace NinetyBackend.Infrastructure.SignalR;

[Authorize]
public sealed class AgentHub(
    IAgentService agentService,
    IMonitoringService monitoringService,
    IAgentConnectionManager connections,
    ILogger<AgentHub> logger) : Hub
{
    private static readonly TimeSpan TelemetryInterval = TimeSpan.FromSeconds(1);

    public async Task<AgentConnectionAccepted> ConnectAgent(Guid stationId, CancellationToken ct = default)
    {
        var agentId = GetAgentId();
        var agent = await agentService.ConnectAsync(agentId, stationId, ct);
        if (agent is null)
            throw new HubException("Connection rejected: agent is not assigned to this station.");

        if (!connections.TryAdd(Context.ConnectionId, agent.Id, agent.StationId))
            throw new HubException("Connection rejected.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(agent.StationId), ct);
        var accepted = new AgentConnectionAccepted(agent.Id, agent.StationId, Context.ConnectionId);
        await Clients.Caller.SendAsync("ConnectionAccepted", accepted, ct);
        await Clients.Group(GroupName(agent.StationId)).SendAsync(
            "StationStatusChanged", new StationStatusChanged(agent.StationId, "Available"), ct);
        return accepted;
    }

    public async Task Heartbeat(CancellationToken ct = default)
    {
        var connection = RequireConnection();
        if (await agentService.HeartbeatAsync(connection.AgentId) is null)
            throw new HubException("Agent not registered.");

        await Clients.Caller.SendAsync("ServerNotification",
            new ServerNotification("HeartbeatAccepted", DateTime.UtcNow), ct);
    }

    public async Task SendTelemetry(TelemetryDto telemetry, CancellationToken ct = default)
    {
        var connection = RequireConnection();
        ValidateTelemetry(telemetry);
        if (telemetry.StationId != connection.StationId)
            throw new HubException("Agent is not assigned to this station.");

        if (!connections.TryAcceptTelemetry(Context.ConnectionId, TelemetryInterval))
            throw new HubException("Telemetry rate limit exceeded.");

        await monitoringService.ProcessTelemetryAsync(connection.StationId, telemetry, ct);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (connections.TryRemove(Context.ConnectionId, out var connection, out var lastConnection) && lastConnection)
        {
            await agentService.DisconnectAsync(connection.AgentId);
            await Clients.Group(GroupName(connection.StationId)).SendAsync(
                "StationStatusChanged", new StationStatusChanged(connection.StationId, "Offline"));
        }

        if (exception is not null)
            logger.LogWarning("Agent connection ended unexpectedly: {ConnectionId}", Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    private AgentConnection RequireConnection()
    {
        if (!connections.TryGet(Context.ConnectionId, out var connection))
            throw new HubException("Agent must connect before calling this method.");
        return connection;
    }

    private Guid GetAgentId()
    {
        var value = Context.User?.FindFirstValue("agent_id");
        return Guid.TryParse(value, out var agentId) && agentId != Guid.Empty
            ? agentId
            : throw new HubException("Connection rejected: agent identity is missing.");
    }

    private static void ValidateTelemetry(TelemetryDto telemetry)
    {
        if (double.IsNaN(telemetry.CpuUsage) || double.IsInfinity(telemetry.CpuUsage) || telemetry.CpuUsage is < 0 or > 100 ||
            double.IsNaN(telemetry.GpuUsage) || double.IsInfinity(telemetry.GpuUsage) || telemetry.GpuUsage is < 0 or > 100 ||
            double.IsNaN(telemetry.RamUsage) || double.IsInfinity(telemetry.RamUsage) || telemetry.RamUsage is < 0 or > 100 ||
            double.IsNaN(telemetry.CpuTemperature) || double.IsInfinity(telemetry.CpuTemperature) || telemetry.CpuTemperature is < -50 or > 150 ||
            double.IsNaN(telemetry.GpuTemperature) || double.IsInfinity(telemetry.GpuTemperature) || telemetry.GpuTemperature is < -50 or > 150 ||
            double.IsNaN(telemetry.FanSpeed) || double.IsInfinity(telemetry.FanSpeed) || telemetry.FanSpeed < 0)
            throw new HubException("Invalid telemetry.");
    }

    private static string GroupName(Guid stationId) => $"station:{stationId}";
}

public sealed record AgentConnectionAccepted(Guid AgentId, Guid StationId, string ConnectionId);
public sealed record StationStatusChanged(Guid StationId, string Status);
public sealed record ServerNotification(string Event, DateTime ServerTimeUtc);
