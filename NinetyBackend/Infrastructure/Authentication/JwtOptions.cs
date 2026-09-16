namespace NinetyBackend.Infrastructure.Authentication;

public class JwtOptions
{
    public string Issuer { get; set; } = "NinetyBackend";
    public string Audience { get; set; } = "NinetyBackendClient";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
