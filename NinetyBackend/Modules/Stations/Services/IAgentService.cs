using NinetyBackend.Modules.Stations.DTOs;

namespace NinetyBackend.Modules.Stations.Services;

public interface IAgentService
{
    Task<AgentResponseDto> RegisterAsync(RegisterAgentDto dto);
    Task<AgentResponseDto?> HeartbeatAsync(Guid id);
    Task<bool> DisconnectAsync(Guid id);
}
