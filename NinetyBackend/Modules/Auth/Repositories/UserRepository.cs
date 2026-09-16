using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;

    public UserRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return _db.Users
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return _db.Users
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x =>
                x.Email.ToLower() == email.ToLower());
    }

    public Task<User?> GetWithRolesAsync(Guid id)
    {
        return _db.Users
            .Include(x => x.Roles)
                .ThenInclude(x => x.Permissions)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<User?> GetByExternalLoginAsync(
        string provider,
        string providerUserId)
    {
        return _db.Users
            .Include(x => x.ExternalLogins)
            .FirstOrDefaultAsync(x =>
                x.ExternalLogins.Any(e =>
                    e.Provider == provider &&
                    e.ProviderUserId == providerUserId));
    }

    public async Task CreateAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }
}
