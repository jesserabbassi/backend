using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.Stations.Models;

namespace NinetyBackend.Modules.Stations.Repositories;

public class StationRepository : IStationRepository
{
    private readonly ApplicationDbContext _db;

    public StationRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<GamingStation>> GetAllAsync()
    {
        return await _db.GamingStations.ToListAsync();
    }

    public Task<GamingStation?> GetByIdAsync(Guid id)
    {
        return _db.GamingStations.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddAsync(GamingStation entity)
    {
        _db.GamingStations.Add(entity);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(GamingStation entity)
    {
        _db.GamingStations.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(GamingStation entity)
    {
        _db.GamingStations.Remove(entity);
        await _db.SaveChangesAsync();
    }
}
