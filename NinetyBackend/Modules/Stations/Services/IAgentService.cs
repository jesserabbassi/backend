using NinetyBackend.Modules.Stations.DTOs;

namespace NinetyBackend.Modules.Stations.Services;

public interface IAgentService
{
    Task<AgentResponseDto> RegisterAsync(RegisterAgentDto dto);
    Task<AgentResponseDto?> ConnectAsync(Guid agentId, Guid stationId, CancellationToken ct = default);
    Task<AgentResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AgentResponseDto?> HeartbeatAsync(Guid id);
    Task<bool> DisconnectAsync(Guid id, CancellationToken ct = default);
}
