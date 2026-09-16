using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Services;

public interface IOAuthService
{
    Task<User> ProcessGoogleLoginAsync(string email, string providerKey, string? firstName, string? lastName);
}
