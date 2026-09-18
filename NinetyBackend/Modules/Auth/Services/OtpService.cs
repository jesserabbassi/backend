using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using NinetyBackend.Infrastructure.Email;
using NinetyBackend.Infrastructure.Security;
using NinetyBackend.Modules.Auth.Models;
using NinetyBackend.Modules.Auth.Repositories;

namespace NinetyBackend.Modules.Auth.Services;

public class OtpService : IOtpService
{
    private readonly IOtpRepository _otpRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public OtpService(IOtpRepository otpRepository, IEmailService emailService, IConfiguration configuration)
    {
        _otpRepository = otpRepository;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task SendOtpAsync(User user)
    {
        var expMinutesStr = _configuration["OTP_EXPIRATION_MINUTES"] ?? _configuration["Otp:ExpirationMinutes"];
        int expMinutes = int.TryParse(expMinutesStr, out var m) ? m : 10;

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var otp = new OtpVerification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = SecurityUtils.HashToken(code),
            ExpiresAt = DateTime.UtcNow.AddMinutes(expMinutes),
            Attempts = 0,
            Verified = false,
            CreatedAt = DateTime.UtcNow
        };

        await _otpRepository.CreateAsync(otp);
        await _emailService.SendOtpEmailAsync(user.Email, code);
    }

    public async Task<bool> VerifyOtpAsync(Guid userId, string code)
    {
        var maxAttemptsStr = _configuration["OTP_MAX_ATTEMPTS"] ?? _configuration["Otp:MaxAttempts"];
        int maxAttempts = int.TryParse(maxAttemptsStr, out var att) ? att : 5;

        var otp = await _otpRepository.GetLatestByUserIdAsync(userId);
        if (otp == null || otp.Verified || otp.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        if (otp.Attempts >= maxAttempts)
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
