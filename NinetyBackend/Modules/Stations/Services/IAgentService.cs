using NinetyBackend.Modules.Stations.DTOs;

namespace NinetyBackend.Modules.Stations.Services;

public interface IAgentService
{
    Task<AgentRegistrationResponseDto> RegisterAsync(RegisterAgentDto dto);
    Task<IReadOnlyList<AgentResponseDto>> GetPendingAsync();
    Task<AgentResponseDto?> ApproveAsync(Guid id);
    Task<AgentResponseDto?> RejectAsync(Guid id);
    Task<AgentResponseDto?> RevokeAsync(Guid id);
    Task<AgentTokenResponseDto?> ExchangeBootstrapAsync(Guid id, string bootstrapSecret, string? ipAddress = null);
    Task<AgentResponseDto?> ConnectAsync(Guid agentId, Guid stationId, CancellationToken ct = default);
    Task<AgentResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AgentResponseDto?> HeartbeatAsync(Guid id);
    Task<bool> DisconnectAsync(Guid id, CancellationToken ct = default);
    Task<AgentTokenResponseDto> IssueTokensAsync(Guid agentId, string? ipAddress = null);
    Task<AgentTokenResponseDto?> RefreshTokenAsync(string rawRefreshToken, string? ipAddress = null);
    Task RevokeRefreshTokenAsync(string rawRefreshToken);
}
