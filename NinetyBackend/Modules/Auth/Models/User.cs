namespace NinetyBackend.Modules.Auth.Models;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsVerified { get; set; }

    public UserStatus Status { get; set; } = UserStatus.PENDING;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public ICollection<Role> Roles { get; set; } = new List<Role>();

    public ICollection<RefreshToken> RefreshTokens { get; set; }
        = new List<RefreshToken>();

    public ICollection<ExternalLogin> ExternalLogins { get; set; }
        = new List<ExternalLogin>();
}

public enum UserStatus
{
    ACTIVE,
    BLOCKED,
    SUSPENDED,
    PENDING
}
