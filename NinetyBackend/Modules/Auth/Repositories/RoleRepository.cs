using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _db;

    public RoleRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Role?> GetByNameAsync(string name)
    {
        return _db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
    }

    public Task<List<Role>> GetAllAsync()
    {
        return _db.Roles.ToListAsync();
    }
}
