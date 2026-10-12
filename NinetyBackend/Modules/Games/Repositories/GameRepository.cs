using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.Games.Models;

namespace NinetyBackend.Modules.Games.Repositories;

public class GameRepository(ApplicationDbContext db) : IGameRepository
{
    public async Task<IEnumerable<Game>> GetAllAsync()
    {
        return await db.Games
            .OrderBy(game => game.Name)
            .ToListAsync();
    }

    public Task<Game?> GetByIdAsync(Guid id)
    {
        return db.Games.FirstOrDefaultAsync(game => game.Id == id);
    }

    public async Task AddAsync(Game game)
    {
        db.Games.Add(game);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Game game)
    {
        db.Games.Update(game);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Game game)
    {
        db.Games.Update(game);
        await db.SaveChangesAsync();
    }
}