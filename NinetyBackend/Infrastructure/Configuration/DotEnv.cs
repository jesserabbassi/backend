using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace NinetyBackend.Infrastructure.Configuration;

public static class DotEnv
{
    public static Dictionary<string, string?> Load(string filePath = ".env")
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var targetPath = ResolveFilePath(filePath);
        if (targetPath == null || !File.Exists(targetPath))
        {
            return result;
        }

        foreach (var line in File.ReadAllLines(targetPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                continue;

            var parts = trimmed.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                continue;

            var key = parts[0];
            var value = parts[1];

            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value.Substring(1, value.Length - 2);
            }

            Environment.SetEnvironmentVariable(key, value);
            result[key] = value;

            MapKeyAliases(key, value, result);
        }

        return result;
    }

    public static Dictionary<string, string?> ExpandPlaceholders(IConfiguration configuration, Dictionary<string, string?> envData)
    {
        var expanded = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in configuration.AsEnumerable())
        {
            if (string.IsNullOrEmpty(pair.Value))
                continue;

            var value = pair.Value;

            var regex = new Regex(@"%([A-Za-z0-9_]+)%|\$\{([A-Za-z0-9_]+)\}");
            var newValue = regex.Replace(value, match =>
            {
                var varName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                var envVal = Environment.GetEnvironmentVariable(varName)
                    ?? (envData.TryGetValue(varName, out var v) ? v : null);
                return envVal ?? match.Value;
            });

            if (newValue != value)
            {
                expanded[pair.Key] = newValue;
            }
        }

        return expanded;
    }

    private static string? ResolveFilePath(string filePath)
    {
        if (File.Exists(filePath)) return filePath;

        var baseDirFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
        if (File.Exists(baseDirFile)) return baseDirFile;

        var currentDirFile = Path.Combine(Directory.GetCurrentDirectory(), filePath);
        if (File.Exists(currentDirFile)) return currentDirFile;

        return null;
    }

    private static void MapKeyAliases(string key, string value, Dictionary<string, string?> result)
    {
        switch (key.ToUpperInvariant())
        {
            case "SUPABASE_CONNECTION_STRING":
                SetAlias("ConnectionStrings:DefaultConnection", value, result);
                SetAlias("ConnectionStrings:Supabase", value, result);
                break;
            case "JWT_SECRET_KEY":
                SetAlias("Jwt:SecretKey", value, result);
                break;
            case "JWT_ISSUER":
                SetAlias("Jwt:Issuer", value, result);
                break;
            case "JWT_AUDIENCE":
                SetAlias("Jwt:Audience", value, result);
                break;
            case "JWT_ACCESS_TOKEN_MINUTES":
                SetAlias("Jwt:AccessTokenMinutes", value, result);
                break;
            case "JWT_REFRESH_TOKEN_DAYS":
                SetAlias("Jwt:RefreshTokenDays", value, result);
                break;
            case "GOOGLE_CLIENT_ID":
                SetAlias("Authentication:Google:ClientId", value, result);
                break;
            case "GOOGLE_CLIENT_SECRET":
                SetAlias("Authentication:Google:ClientSecret", value, result);
                break;
            case "SMTP_HOST":
                SetAlias("Smtp:Host", value, result);
                SetAlias("Email:Host", value, result);
                break;
            case "SMTP_PORT":
                SetAlias("Smtp:Port", value, result);
                SetAlias("Email:Port", value, result);
                break;
            case "SMTP_USERNAME":
                SetAlias("Smtp:Username", value, result);
                SetAlias("Email:Username", value, result);
                break;
            case "SMTP_PASSWORD":
                SetAlias("Smtp:Password", value, result);
                SetAlias("Email:Password", value, result);
                break;
            case "SMTP_FROM":
                SetAlias("Smtp:From", value, result);
                SetAlias("Email:From", value, result);
                break;
            case "OTP_EXPIRATION_MINUTES":
                SetAlias("Otp:ExpirationMinutes", value, result);
                break;
            case "OTP_MAX_ATTEMPTS":
                SetAlias("Otp:MaxAttempts", value, result);
                break;
        }
    }

    private static void SetAlias(string aliasKey, string value, Dictionary<string, string?> result)
    {
        result[aliasKey] = value;
        var envKey = aliasKey.Replace(":", "__");
        Environment.SetEnvironmentVariable(envKey, value);
    }
}
