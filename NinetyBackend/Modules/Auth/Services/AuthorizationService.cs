using NinetyBackend.Modules.Auth.Repositories;

namespace NinetyBackend.Modules.Auth.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly IUserRepository _userRepository;

    public AuthorizationService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, string permissionName)
    {
        var user = await _userRepository.GetWithRolesAsync(userId);
        if (user == null || user.Roles == null) return false;

        return user.Roles.Any(r =>
            r.Permissions != null &&
            r.Permissions.Any(p => p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<bool> UserHasRoleAsync(Guid userId, string roleName)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.Roles == null) return false;

        return user.Roles.Any(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }
}
