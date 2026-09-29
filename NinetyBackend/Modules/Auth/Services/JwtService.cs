using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NinetyBackend.Infrastructure.Authentication;
using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Services;

public class JwtService : IJwtService
{
    private readonly JwtOptions _options;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email)
        };

        if (user.Roles != null)
        {
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
                if (role.Permissions != null)
                {
                    foreach (var perm in role.Permissions)
                    {
                        claims.Add(new Claim("permission", perm.Name));
                    }
                }
            }
        }

        return GenerateToken(claims);
    }

    public string GenerateAgentAccessToken(Guid agentId)
    {
        if (agentId == Guid.Empty)
            throw new ArgumentException("Agent id is required.", nameof(agentId));

        return GenerateToken([new Claim("agent_id", agentId.ToString())]);
    }

    private string GenerateToken(IEnumerable<Claim> claims)
    {
        var secret = string.IsNullOrEmpty(_options.SecretKey)
            ? "SuperSecretDefaultKeyMustBeAtLeast32BytesLongForSecurity!"
            : _options.SecretKey;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes > 0 ? _options.AccessTokenMinutes : 15);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
