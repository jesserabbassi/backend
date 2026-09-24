using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.Stations.Models;

namespace NinetyBackend.Modules.Stations.Repositories;

public class AgentRepository : IAgentRepository
{
    private readonly ApplicationDbContext _db;

    public AgentRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Agent?> GetByIdAsync(Guid id)
    {
        return _db.Agents.FirstOrDefaultAsync(a => a.Id == id);
    }

    public Task<Agent?> GetByStationIdAsync(Guid stationId)
    {
        return _db.Agents.FirstOrDefaultAsync(a => a.StationId == stationId);
    }

    public async Task AddAsync(Agent entity)
    {
        _db.Agents.Add(entity);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Agent entity)
    {
        _db.Agents.Update(entity);
        await _db.SaveChangesAsync();
    }
}
