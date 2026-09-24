using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Repositories;

public interface IOtpRepository
{
    Task<OtpVerification?> GetLatestByUserIdAndPurposeAsync(Guid userId, OtpPurpose purpose);
    Task InvalidateActiveOtpsAsync(Guid userId, OtpPurpose purpose);
    Task CreateAsync(OtpVerification otp);
    Task UpdateAsync(OtpVerification otp);
}
