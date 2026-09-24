using NinetyBackend.Infrastructure.Security;
using NinetyBackend.Modules.Auth.DTOs;
using NinetyBackend.Modules.Auth.Models;
using NinetyBackend.Modules.Auth.Repositories;

namespace NinetyBackend.Modules.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthService(
        IUserRepository userRepository,
        IOtpService otpService,
        IJwtService jwtService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var existing = await _userRepository.GetByEmailAsync(dto.Email);
        if (existing != null)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = SecurityUtils.HashPassword(dto.Password),
            IsVerified = false,
            Status = UserStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);
        await _otpService.SendOtpAsync(user, OtpPurpose.Registration);

        return new AuthResponseDto(user.Id, user.Email, RequiresOtp: true);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ipAddress = null)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null || !SecurityUtils.VerifyPassword(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (user.Status == UserStatus.BLOCKED || user.Status == UserStatus.SUSPENDED)
        {
            throw new UnauthorizedAccessException("Account is disabled.");
        }

        // Unverified accounts still require registration OTP first
        if (!user.IsVerified || user.Status == UserStatus.PENDING)
        {
            await _otpService.SendOtpAsync(user, OtpPurpose.Registration);
            return new AuthResponseDto(user.Id, user.Email, RequiresOtp: true);
        }

        // Every verified login requires a login OTP — tokens are never issued here
        await _otpService.SendOtpAsync(user, OtpPurpose.Login);
        return new AuthResponseDto(user.Id, user.Email, RequiresOtp: true);
    }

    public async Task<AuthResponseDto?> VerifyOtpAsync(VerifyOtpDto dto, string? ipAddress = null)
    {
        var verified = await _otpService.VerifyOtpAsync(dto.UserId, dto.Code, dto.Purpose);
        if (!verified)
        {
            return null;
        }

        var user = await _userRepository.GetWithRolesAsync(dto.UserId);
        if (user == null)
        {
            return null;
        }

        if (dto.Purpose == OtpPurpose.Registration)
        {
            // Registration OTP: activate the account
            user.IsVerified = true;
            if (user.Status == UserStatus.PENDING)
            {
                user.Status = UserStatus.ACTIVE;
            }
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
        }
        else if (dto.Purpose == OtpPurpose.Login)
        {
            // Login OTP: just record the login timestamp — account already verified
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
        }

        return await GenerateAuthResponseForUserAsync(user, ipAddress);
    }

    /// <summary>
    /// Completes authentication for a Google OAuth user.
    /// Google has already verified the user's identity, so no OTP is required.
    /// </summary>
    public async Task<AuthResponseDto> CompleteGoogleLoginAsync(Guid userId, string? ipAddress = null)
    {
        var user = await _userRepository.GetWithRolesAsync(userId);
        if (user == null || user.Status == UserStatus.BLOCKED || user.Status == UserStatus.SUSPENDED)
        {
            throw new UnauthorizedAccessException("Account is disabled or not found.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return await GenerateAuthResponseForUserAsync(user, ipAddress);
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(string rawRefreshToken, string? ipAddress = null)
    {
        var tokenHash = SecurityUtils.HashToken(rawRefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken == null)
        {
            return null;
        }

        if (storedToken.RevokedAt != null)
        {
            await _refreshTokenRepository.RevokeAllUserTokensAsync(storedToken.UserId);
            return null;
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        storedToken.RevokedAt = DateTime.UtcNow;

        var user = await _userRepository.GetWithRolesAsync(storedToken.UserId);
        if (user == null || user.Status == UserStatus.BLOCKED || user.Status == UserStatus.SUSPENDED)
        {
            await _refreshTokenRepository.UpdateAsync(storedToken);
            return null;
        }

        var newRawRefreshToken = _jwtService.GenerateRefreshToken();
        var newTokenHash = SecurityUtils.HashToken(newRawRefreshToken);

        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedByIp = ipAddress
        };

        storedToken.ReplacedByTokenId = newToken.Id;
        await _refreshTokenRepository.UpdateAsync(storedToken);
        await _refreshTokenRepository.CreateAsync(newToken);

        var accessToken = _jwtService.GenerateAccessToken(user);

        return new AuthResponseDto(
            user.Id,
            user.Email,
            RequiresOtp: false,
            AccessToken: accessToken,
            RefreshToken: newRawRefreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(15)
        );
    }

    public async Task RevokeTokenAsync(string rawRefreshToken)
    {
        var tokenHash = SecurityUtils.HashToken(rawRefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);
        if (storedToken != null && storedToken.RevokedAt == null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(storedToken);
        }
    }

    public async Task<User?> GetUserProfileAsync(Guid userId)
    {
        return await _userRepository.GetWithRolesAsync(userId);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !SecurityUtils.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = SecurityUtils.HashPassword(dto.NewPassword);
        await _userRepository.UpdateAsync(user);
        return true;
    }

    private async Task<AuthResponseDto> GenerateAuthResponseForUserAsync(User user, string? ipAddress)
    {
        var userWithRoles = await _userRepository.GetWithRolesAsync(user.Id) ?? user;
        var accessToken = _jwtService.GenerateAccessToken(userWithRoles);
        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = SecurityUtils.HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedByIp = ipAddress
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

        return new AuthResponseDto(
            user.Id,
            user.Email,
            RequiresOtp: false,
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(15)
        );
    }
}
