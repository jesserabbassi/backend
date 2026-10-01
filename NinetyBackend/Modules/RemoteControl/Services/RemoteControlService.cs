using System.Text.Json;
using NinetyBackend.Modules.RemoteControl.Models;
using NinetyBackend.Modules.RemoteControl.Repositories;
using NinetyBackend.Modules.Stations.Repositories;

namespace NinetyBackend.Modules.RemoteControl.Services;

public interface IAgentConnectionService
{
    bool IsConnected(Guid stationId);
    Task<bool> SendCommandAsync(Guid stationId, RemoteCommand command, CancellationToken ct = default);
}

public interface IRemoteControlService
{
    Task<CommandResultDto> SendCommandAsync(Guid stationId, CommandType type, object? payload, CancellationToken ct = default);
    Task<CommandResultDto?> GetByIdAsync(Guid commandId, CancellationToken ct = default);
    Task<List<CommandResultDto>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct = default);
    Task<CommandResultDto?> AcknowledgeAsync(Guid commandId, bool success, string? message = null, CancellationToken ct = default);
}

public class InMemoryAgentConnectionService : IAgentConnectionService
{
    private readonly HashSet<Guid> _connectedStations = [];

    public bool IsConnected(Guid stationId) => _connectedStations.Contains(stationId);

    public Task<bool> SendCommandAsync(Guid stationId, RemoteCommand command, CancellationToken ct = default)
    {
        if (!_connectedStations.Contains(stationId))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public void MarkConnected(Guid stationId) => _connectedStations.Add(stationId);
    public void MarkDisconnected(Guid stationId) => _connectedStations.Remove(stationId);
}

public class RemoteControlService(
    ICommandRepository commandRepository,
    IAgentConnectionService agentConnectionService,
    IStationRepository stationRepository) : IRemoteControlService
{
    public async Task<CommandResultDto> SendCommandAsync(Guid stationId, CommandType type, object? payload, CancellationToken ct = default)
    {
        var station = await stationRepository.GetByIdAsync(stationId);
        if (station is null)
        {
            throw new KeyNotFoundException("Station not found.");
        }

        if (!agentConnectionService.IsConnected(stationId))
        {
            throw new InvalidOperationException("Station agent is not connected.");
        }

        var command = new RemoteCommand
        {
            StationId = stationId,
            Type = type,
            Status = CommandStatus.Pending,
            PayloadJson = payload is null ? null : JsonSerializer.Serialize(payload),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        await commandRepository.AddAsync(command, ct);

        var sent = await agentConnectionService.SendCommandAsync(stationId, command, ct);
        command.Status = sent ? CommandStatus.Sent : CommandStatus.Failed;
        command.SentAt = DateTime.UtcNow;
        command.ResultMessage = sent ? "Command sent to station agent." : "Failed to deliver command to station agent.";
        await commandRepository.UpdateAsync(command, ct);

        return Map(command);
    }

    public async Task<CommandResultDto?> GetByIdAsync(Guid commandId, CancellationToken ct = default)
    {
        var command = await commandRepository.GetByIdAsync(commandId, ct);
        return command is null ? null : Map(command);
    }

    public async Task<List<CommandResultDto>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct = default)
    {
        var commands = await commandRepository.GetHistoryAsync(stationId, Math.Clamp(take, 1, 200), ct);
        return commands.Select(Map).ToList();
    }

    public async Task<CommandResultDto?> AcknowledgeAsync(Guid commandId, bool success, string? message = null, CancellationToken ct = default)
    {
        var command = await commandRepository.GetByIdAsync(commandId, ct);
        if (command is null)
        {
            return null;
        }

        command.Status = success ? CommandStatus.Acknowledged : CommandStatus.Failed;
        command.AcknowledgedAt = DateTime.UtcNow;
        command.ResultMessage = message ?? (success ? "Command acknowledged by station agent." : "Command rejected by station agent.");
        await commandRepository.UpdateAsync(command, ct);

        return Map(command);
    }

    private static CommandResultDto Map(RemoteCommand command) => new(
        command.Id,
        command.StationId,
        command.Type,
        command.Status,
        command.CreatedAt,
        command.SentAt,
        command.AcknowledgedAt,
        command.ResultMessage);
}
