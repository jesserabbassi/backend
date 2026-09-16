using System.Security.Cryptography;
using NinetyBackend.Infrastructure.Email;
using NinetyBackend.Infrastructure.Security;
using NinetyBackend.Modules.Auth.Models;
using NinetyBackend.Modules.Auth.Repositories;

namespace NinetyBackend.Modules.Auth.Services;

public class OtpService : IOtpService
{
    private readonly IOtpRepository _otpRepository;
    private readonly IEmailService _emailService;

    public OtpService(IOtpRepository otpRepository, IEmailService emailService)
    {
        _otpRepository = otpRepository;
        _emailService = emailService;
    }

    public async Task SendOtpAsync(User user)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var otp = new OtpVerification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = SecurityUtils.HashToken(code),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Attempts = 0,
            Verified = false,
            CreatedAt = DateTime.UtcNow
        };

        await _otpRepository.CreateAsync(otp);
        await _emailService.SendOtpEmailAsync(user.Email, code);
    }

    public async Task<bool> VerifyOtpAsync(Guid userId, string code)
    {
        var otp = await _otpRepository.GetLatestByUserIdAsync(userId);
        if (otp == null || otp.Verified || otp.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        if (otp.Attempts >= 5)
        {
            return false;
        }

        otp.Attempts++;
        var codeHash = SecurityUtils.HashToken(code);
        if (otp.CodeHash != codeHash)
        {
            await _otpRepository.UpdateAsync(otp);
            return false;
        }

        otp.Verified = true;
        await _otpRepository.UpdateAsync(otp);
        return true;
    }
}
