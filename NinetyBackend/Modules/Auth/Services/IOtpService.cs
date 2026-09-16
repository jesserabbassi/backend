using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Services;

public interface IOtpService
{
    Task SendOtpAsync(User user);
    Task<bool> VerifyOtpAsync(Guid userId, string code);
}
