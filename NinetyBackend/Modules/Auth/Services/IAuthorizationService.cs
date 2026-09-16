namespace NinetyBackend.Modules.Auth.Services;

public interface IAuthorizationService
{
    Task<bool> UserHasPermissionAsync(Guid userId, string permissionName);
    Task<bool> UserHasRoleAsync(Guid userId, string roleName);
}
