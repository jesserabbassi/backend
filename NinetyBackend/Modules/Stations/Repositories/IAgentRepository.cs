using NinetyBackend.Modules.Stations.Models;

namespace NinetyBackend.Modules.Stations.Repositories;

public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(Guid id);
    Task<Agent?> GetByStationIdAsync(Guid stationId);
    Task AddAsync(Agent entity);
    Task UpdateAsync(Agent entity);
}
