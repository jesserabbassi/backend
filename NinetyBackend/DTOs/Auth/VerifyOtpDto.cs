namespace NinetyBackend.DTOs.Auth
{
    public record VerifyOtpDto
    {
        Guid UserId { get; init; }
        string code { get; init; }
    }
}
