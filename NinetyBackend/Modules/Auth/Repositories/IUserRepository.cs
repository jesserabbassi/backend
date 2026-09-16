using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);

    Task<User?> GetByEmailAsync(string email);

    Task<User?> GetWithRolesAsync(Guid id);

    Task<User?> GetByExternalLoginAsync(
        string provider,
        string providerUserId);

    Task CreateAsync(User user);

    Task UpdateAsync(User user);
}
