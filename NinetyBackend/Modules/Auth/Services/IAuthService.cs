using NinetyBackend.Modules.Auth.DTOs;
using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ipAddress = null);
    Task<AuthResponseDto?> VerifyOtpAsync(VerifyOtpDto dto, string? ipAddress = null);
    Task<AuthResponseDto> CompleteGoogleLoginAsync(Guid userId, string? ipAddress = null);
    Task<AuthResponseDto?> RefreshTokenAsync(string rawRefreshToken, string? ipAddress = null);
    Task RevokeTokenAsync(string rawRefreshToken);
    Task<User?> GetUserProfileAsync(Guid userId);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
}
