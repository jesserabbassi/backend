using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
