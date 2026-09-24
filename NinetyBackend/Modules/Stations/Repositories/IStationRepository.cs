using NinetyBackend.Modules.Stations.Models;

namespace NinetyBackend.Modules.Stations.Repositories;

public interface IStationRepository
{
    Task<IEnumerable<GamingStation>> GetAllAsync();
    Task<GamingStation?> GetByIdAsync(Guid id);
    Task AddAsync(GamingStation entity);
    Task UpdateAsync(GamingStation entity);
    Task DeleteAsync(GamingStation entity);
}
