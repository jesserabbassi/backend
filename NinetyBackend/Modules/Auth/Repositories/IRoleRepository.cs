using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name);
    Task<List<Role>> GetAllAsync();
}
