namespace NinetyBackend.Modules.Auth.DTOs;

public class VerifyOtpDto
{
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
}
