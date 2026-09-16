using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Repositories;

public interface IOtpRepository
{
    Task<OtpVerification?> GetLatestByUserIdAsync(Guid userId);
    Task CreateAsync(OtpVerification otp);
    Task UpdateAsync(OtpVerification otp);
}
