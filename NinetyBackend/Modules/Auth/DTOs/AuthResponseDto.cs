namespace NinetyBackend.Modules.Auth.DTOs;

public record AuthResponseDto(
    Guid UserId,
    string Email,
    bool RequiresOtp,
    string? AccessToken = null,
    string? RefreshToken = null,
    DateTime? ExpiresAt = null
);
