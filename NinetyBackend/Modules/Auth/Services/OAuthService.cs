using NinetyBackend.Modules.Auth.Models;
using NinetyBackend.Modules.Auth.Repositories;
using NinetyBackend.Modules.Auth.Authorization;

namespace NinetyBackend.Modules.Auth.Services;

public class OAuthService : IOAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public OAuthService(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
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

        var defaultRole = await _roleRepository.GetDefaultRoleAsync()
            ?? throw new InvalidOperationException("Default Customer role is not configured.");
        newUser.Roles.Add(defaultRole);

        await _userRepository.CreateAsync(newUser);
        return newUser;
    }
}
