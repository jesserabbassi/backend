using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.DTOs;

public class VerifyOtpDto
{
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Purpose of the OTP being verified. Defaults to Login for normal post-login verification.
    /// Use Registration when verifying a new account.
    /// </summary>
    public OtpPurpose Purpose { get; set; } = OtpPurpose.Login;
}
