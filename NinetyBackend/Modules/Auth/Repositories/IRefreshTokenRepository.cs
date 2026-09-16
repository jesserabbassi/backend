using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId);
    Task CreateAsync(RefreshToken token);
    Task UpdateAsync(RefreshToken token);
    Task RevokeAllUserTokensAsync(Guid userId);
}
