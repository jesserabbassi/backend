using NinetyBackend.Modules.RemoteControl.Models;

namespace NinetyBackend.Modules.RemoteControl.Repositories;

public interface ICommandRepository
{
    Task<RemoteCommand> AddAsync(RemoteCommand command, CancellationToken ct = default);
    Task<RemoteCommand?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<RemoteCommand>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct = default);
    Task UpdateAsync(RemoteCommand command, CancellationToken ct = default);
}
