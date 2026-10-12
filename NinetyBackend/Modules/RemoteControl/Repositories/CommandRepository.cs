using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.RemoteControl.Models;

namespace NinetyBackend.Modules.RemoteControl.Repositories;

public class CommandRepository(ApplicationDbContext db) : ICommandRepository
{
    public async Task<RemoteCommand> AddAsync(RemoteCommand command, CancellationToken ct = default)
    {
        db.RemoteCommands.Add(command);
        await db.SaveChangesAsync(ct);
        return command;
    }

    public async Task<RemoteCommand?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.RemoteCommands.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<RemoteCommand>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct = default)
    {
        return await db.RemoteCommands
            .Where(c => c.StationId == stationId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(RemoteCommand command, CancellationToken ct = default)
    {
        db.RemoteCommands.Update(command);
        await db.SaveChangesAsync(ct);
    }
}
