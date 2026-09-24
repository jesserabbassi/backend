using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using NinetyGamingStationBackend.Application.DTOs.RemoteControl;
using NinetyGamingStationBackend.Application.Interfaces;
using NinetyGamingStationBackend.Domain.Entities;
using NinetyGamingStationBackend.Domain.Enums;
using NinetyGamingStationBackend.Realtime;

namespace NinetyGamingStationBackend.Application.Services;

public sealed class RemoteControlService(
    IRemoteCommandRepository commands,
    IStationRepository stations,
    AgentConnectionManager connections,
    IHubContext<MonitoringHub> hub) : IRemoteControlService
{
    public async Task<CommandResultDto?> ExecuteAsync(Guid stationId, CommandType type, object? payload, CancellationToken ct)
    {
        var station = await stations.GetByIdAsync(stationId, ct);
        if (station is null) return null;
        if (!connections.IsConnected(stationId)) throw new InvalidOperationException("Station agent is not connected.");

        var commandId = Guid.NewGuid();
        var wire = AgentCommandFactory.Create(commandId, type, payload);

        var command = new RemoteCommand
        {
            Id = commandId,
            StationId = stationId,
            Type = type,
            PayloadJson = payload is null ? null : JsonSerializer.Serialize(payload, ProtocolJson.Options)
        };
        await commands.AddAsync(command, ct);
        var sent = await connections.SendAsync(stationId, wire, ct);
        command.Status = sent ? CommandStatus.Sent : CommandStatus.Failed;
        command.SentAt = sent ? DateTime.UtcNow : null;
        command.ResultDetail = sent ? null : "Agent connection was lost before send.";
        await commands.UpdateAsync(command, ct);

        return Map(command);
    }

    public async Task<CommandResultDto?> GetCommandAsync(Guid commandId, CancellationToken ct)
    {
        var c = await commands.GetAsync(commandId, ct);
        return c is null ? null : Map(c);
    }

    public async Task<List<CommandResultDto>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct)
        => (await commands.GetHistoryAsync(stationId, Math.Clamp(take, 1, 200), ct)).Select(Map).ToList();

    public async Task HandleAcknowledgementAsync(Guid stationId, CommandType type, bool success, string? detail, Guid? commandId, CancellationToken ct)
    {
        RemoteCommand? command = commandId.HasValue
            ? await commands.GetAsync(commandId.Value, ct)
            : await commands.FindPendingForAckAsync(stationId, type, ct);

        if (command is null || command.StationId != stationId) return;

        command.Status = success ? CommandStatus.Acknowledged : CommandStatus.Failed;
        command.AcknowledgedAt = DateTime.UtcNow;
        command.ResultDetail = detail;
        await commands.UpdateAsync(command, ct);
        var station = await stations.GetByIdAsync(stationId, ct);
        if (station is not null)
            await hub.Clients.Groups(MonitoringHub.StationGroup(stationId), MonitoringHub.BranchGroup(station.BranchId))
                .SendAsync("command.updated", Map(command), ct);
    }

    private static CommandResultDto Map(RemoteCommand c) =>
        new(c.Id, c.StationId, c.Type, c.Status, c.CreatedAt, c.SentAt, c.AcknowledgedAt, c.ResultDetail);
}

internal static class AgentCommandFactory
{
    public static BackendCommand Create(Guid commandId, CommandType type, object? payload) => type switch
    {
        CommandType.Lock => new LockCommand(commandId),
        CommandType.Unlock => new UnlockCommand(commandId),
        CommandType.Restart => new RestartCommand(commandId),
        CommandType.Shutdown => new ShutdownCommand(commandId),
        CommandType.StartSession => new StartSessionCommand(commandId, ReadString(payload, "session_id")),
        CommandType.EndSession => new EndSessionCommand(commandId, ReadString(payload, "session_id"), ReadString(payload, "reason")),
        CommandType.LaunchGame => new LaunchGameCommand(commandId, ReadString(payload, "executable")),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static string ReadString(object? payload, string name)
    {
        if (payload is null) throw new ArgumentException($"Payload field '{name}' is required.");
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload, ProtocolJson.Options));
        if (!doc.RootElement.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            throw new ArgumentException($"Payload field '{name}' is required.");
        return value.GetString()!;
    }
}
