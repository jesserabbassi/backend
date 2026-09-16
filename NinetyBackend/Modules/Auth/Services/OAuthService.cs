using NinetyBackend.Modules.Auth.Models;
using NinetyBackend.Modules.Auth.Repositories;

namespace NinetyBackend.Modules.Auth.Services;

public class OAuthService : IOAuthService
{
    private readonly IUserRepository _userRepository;

    public OAuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> ProcessGoogleLoginAsync(string email, string providerKey, string? firstName, string? lastName)
    {
        var existingUser = await _userRepository.GetByExternalLoginAsync("Google", providerKey);
        if (existingUser != null)
        {
            existingUser.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(existingUser);
            return existingUser;
        }

        var userByEmail = await _userRepository.GetByEmailAsync(email);
        if (userByEmail != null)
        {
            userByEmail.ExternalLogins.Add(new ExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = userByEmail.Id,
                Provider = "Google",
                ProviderUserId = providerKey
            });
            userByEmail.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(userByEmail);
            return userByEmail;
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = string.Empty,
            IsVerified = true,
            Status = UserStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            ExternalLogins = new List<ExternalLogin>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Provider = "Google",
                    ProviderUserId = providerKey
                }
            }
        };

        await _userRepository.CreateAsync(newUser);
        return newUser;
    }
}
