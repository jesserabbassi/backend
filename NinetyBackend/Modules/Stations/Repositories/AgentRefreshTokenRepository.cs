using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.Stations.Models;

namespace NinetyBackend.Modules.Stations.Repositories;

public class AgentRefreshTokenRepository : IAgentRefreshTokenRepository
{
    private readonly ApplicationDbContext _db;

    public AgentRefreshTokenRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<AgentRefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return _db.AgentRefreshTokens
            .Include(x => x.Agent)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);
    }

    public Task<List<AgentRefreshToken>> GetActiveByAgentIdAsync(Guid agentId)
    {
        return _db.AgentRefreshTokens
            .Where(x => x.AgentId == agentId && x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task CreateAsync(AgentRefreshToken token)
    {
        _db.AgentRefreshTokens.Add(token);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(AgentRefreshToken token)
    {
        _db.AgentRefreshTokens.Update(token);
        await _db.SaveChangesAsync();
    }

    public async Task RevokeAllByAgentIdAsync(Guid agentId)
    {
        var tokens = await GetActiveByAgentIdAsync(agentId);
        foreach (var t in tokens)
            t.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
