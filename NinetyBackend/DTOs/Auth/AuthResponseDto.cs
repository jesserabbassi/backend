namespace NinetyBackend.DTOs.Auth
{
    public record AuthResponseDto(
        Guid UserId,
        string Email,
        bool RequiesOtp
    );
}