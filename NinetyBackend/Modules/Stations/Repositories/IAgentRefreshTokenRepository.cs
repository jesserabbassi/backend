using NinetyBackend.Modules.Stations.Models;

namespace NinetyBackend.Modules.Stations.Repositories;

public interface IAgentRefreshTokenRepository
{
    Task<AgentRefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task<List<AgentRefreshToken>> GetActiveByAgentIdAsync(Guid agentId);
    Task CreateAsync(AgentRefreshToken token);
    Task UpdateAsync(AgentRefreshToken token);
    Task RevokeAllByAgentIdAsync(Guid agentId);
}
