namespace NinetyBackend.Modules.Auth.Models;

public enum OtpPurpose
{
    Registration = 0,
    Login = 1,
    PasswordReset = 2
}

public class OtpVerification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string CodeHash { get; set; } = string.Empty;

    public OtpPurpose Purpose { get; set; } = OtpPurpose.Registration;

    public DateTime ExpiresAt { get; set; }

    public int Attempts { get; set; }

    public bool Verified { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
