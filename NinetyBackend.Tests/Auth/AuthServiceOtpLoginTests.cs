using Moq;
using NinetyBackend.Infrastructure.Security;
using NinetyBackend.Modules.Auth.DTOs;
using NinetyBackend.Modules.Auth.Models;
using NinetyBackend.Modules.Auth.Repositories;
using NinetyBackend.Modules.Auth.Services;
using Xunit;

namespace NinetyBackend.Tests.Auth;

/// <summary>
/// Unit tests for the OTP-gated login flow.
/// All external dependencies (repositories, email, JWT) are mocked.
/// </summary>
public class AuthServiceOtpLoginTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User ActiveVerifiedUser(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = "user@example.com",
        PasswordHash = SecurityUtils.HashPassword("Password123!"),
        IsVerified = true,
        Status = UserStatus.ACTIVE,
        CreatedAt = DateTime.UtcNow
    };

    private static User PendingUnverifiedUser(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = "new@example.com",
        PasswordHash = SecurityUtils.HashPassword("Password123!"),
        IsVerified = false,
        Status = UserStatus.PENDING,
        CreatedAt = DateTime.UtcNow
    };

    private static (AuthService svc,
        Mock<IUserRepository> userRepo,
        Mock<IOtpService> otpSvc,
        Mock<IJwtService> jwtSvc,
        Mock<IRefreshTokenRepository> rtRepo)
        BuildService()
    {
        var userRepo = new Mock<IUserRepository>();
        var otpSvc = new Mock<IOtpService>();
        var jwtSvc = new Mock<IJwtService>();
        var rtRepo = new Mock<IRefreshTokenRepository>();

        jwtSvc.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        jwtSvc.Setup(j => j.GenerateRefreshToken()).Returns("raw-refresh-token");
        rtRepo.Setup(r => r.CreateAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);

        var svc = new AuthService(userRepo.Object, otpSvc.Object, jwtSvc.Object, rtRepo.Object);
        return (svc, userRepo, otpSvc, jwtSvc, rtRepo);
    }

    // ─── Test 1: Valid password login sends a login OTP ───────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_SendsLoginOtp()
    {
        var (svc, userRepo, otpSvc, _, _) = BuildService();
        var user = ActiveVerifiedUser();
        userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        otpSvc.Setup(o => o.SendOtpAsync(user, OtpPurpose.Login)).Returns(Task.CompletedTask);

        await svc.LoginAsync(new LoginDto(user.Email, "Password123!"));

        otpSvc.Verify(o => o.SendOtpAsync(user, OtpPurpose.Login), Times.Once);
    }

    // ─── Test 2: Valid password login returns RequiresOtp = true ──────────────

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsRequiresOtpTrue()
    {
        var (svc, userRepo, otpSvc, _, _) = BuildService();
        var user = ActiveVerifiedUser();
        userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        otpSvc.Setup(o => o.SendOtpAsync(It.IsAny<User>(), It.IsAny<OtpPurpose>())).Returns(Task.CompletedTask);

        var result = await svc.LoginAsync(new LoginDto(user.Email, "Password123!"));

        Assert.True(result.RequiresOtp);
    }

    // ─── Test 3: Valid password login does not issue an access token ──────────

    [Fact]
    public async Task Login_WithValidCredentials_DoesNotIssueAccessToken()
    {
        var (svc, userRepo, otpSvc, _, _) = BuildService();
        var user = ActiveVerifiedUser();
        userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        otpSvc.Setup(o => o.SendOtpAsync(It.IsAny<User>(), It.IsAny<OtpPurpose>())).Returns(Task.CompletedTask);

        var result = await svc.LoginAsync(new LoginDto(user.Email, "Password123!"));

        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
    }

    // ─── Test 4: Invalid password does not send an OTP ───────────────────────

    [Fact]
    public async Task Login_WithWrongPassword_DoesNotSendOtp()
    {
        var (svc, userRepo, otpSvc, _, _) = BuildService();
        var user = ActiveVerifiedUser();
        userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.LoginAsync(new LoginDto(user.Email, "WrongPassword!")));

        otpSvc.Verify(o => o.SendOtpAsync(It.IsAny<User>(), It.IsAny<OtpPurpose>()), Times.Never);
    }

    // ─── Test 5: Blocked user cannot log in ──────────────────────────────────

    [Fact]
    public async Task Login_WithBlockedUser_ThrowsUnauthorized()
    {
        var (svc, userRepo, _, _, _) = BuildService();
        var user = ActiveVerifiedUser();
        user.Status = UserStatus.BLOCKED;
        userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.LoginAsync(new LoginDto(user.Email, "Password123!")));
    }

    // ─── Test 6: Valid login OTP issues access and refresh tokens ─────────────

    [Fact]
    public async Task VerifyOtp_WithValidLoginOtp_IssuesTokens()
    {
        var (svc, userRepo, otpSvc, _, _) = BuildService();
        var userId = Guid.NewGuid();
        var user = ActiveVerifiedUser(userId);
        userRepo.Setup(r => r.GetWithRolesAsync(userId)).ReturnsAsync(user);
        userRepo.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);
        otpSvc.Setup(o => o.VerifyOtpAsync(userId, "123456", OtpPurpose.Login)).ReturnsAsync(true);

        var result = await svc.VerifyOtpAsync(new VerifyOtpDto
        {
            UserId = userId,
            Code = "123456",
            Purpose = OtpPurpose.Login
        });

        Assert.NotNull(result);
        Assert.False(result!.RequiresOtp);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("raw-refresh-token", result.RefreshToken);
    }

    // ─── Test 7: Valid login OTP marks OTP as used (OtpService.VerifyOtp called)

    [Fact]
    public async Task VerifyOtp_WithValidLoginOtp_MarksOtpUsed()
    {
        var (svc, userRepo, otpSvc, _, _) = BuildService();
        var userId = Guid.NewGuid();
        var user = ActiveVerifiedUser(userId);
        userRepo.Setup(r => r.GetWithRolesAsync(userId)).ReturnsAsync(user);
        userRepo.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);
        otpSvc.Setup(o => o.VerifyOtpAsync(userId, "123456", OtpPurpose.Login)).ReturnsAsync(true);

        await svc.VerifyOtpAsync(new VerifyOtpDto { UserId = userId, Code = "123456", Purpose = OtpPurpose.Login });

        otpSvc.Verify(o => o.VerifyOtpAsync(userId, "123456", OtpPurpose.Login), Times.Once);
    }

    // ─── Test 8: Used login OTP cannot be reused ──────────────────────────────

    [Fact]
    public async Task VerifyOtp_WithAlreadyUsedOtp_ReturnsNull()
    {
        var (svc, userRepo, otpSvc, _, _) = BuildService();
        var userId = Guid.NewGuid();
        otpSvc.Setup(o => o.VerifyOtpAsync(userId, "123456", OtpPurpose.Login)).ReturnsAsync(false);

        var result = await svc.VerifyOtpAsync(new VerifyOtpDto
        {
            UserId = userId,
            Code = "123456",
            Purpose = OtpPurpose.Login
        });

        Assert.Null(result);
    }

    // ─── Test 9: Expired login OTP is rejected ────────────────────────────────

    [Fact]
    public async Task VerifyOtp_WithExpiredOtp_ReturnsNull()
    {
        var (svc, _, otpSvc, _, _) = BuildService();
        var userId = Guid.NewGuid();
        // OtpService returns false when OTP is expired
        otpSvc.Setup(o => o.VerifyOtpAsync(userId, "999999", OtpPurpose.Login)).ReturnsAsync(false);

        var result = await svc.VerifyOtpAsync(new VerifyOtpDto
        {
            UserId = userId,
            Code = "999999",
            Purpose = OtpPurpose.Login
        });

        Assert.Null(result);
    }

    // ─── Test 10: Registration OTP cannot be used as login OTP ───────────────

    [Fact]
    public async Task VerifyOtp_RegistrationOtpCode_CannotBeUsedAsLoginOtp()
    {
        // OtpService scopes by purpose — querying Login will not find the Registration OTP
        var (svc, _, otpSvc, _, _) = BuildService();
        var userId = Guid.NewGuid();
        otpSvc.Setup(o => o.VerifyOtpAsync(userId, "111111", OtpPurpose.Registration)).ReturnsAsync(true);
        otpSvc.Setup(o => o.VerifyOtpAsync(userId, "111111", OtpPurpose.Login)).ReturnsAsync(false);

        var result = await svc.VerifyOtpAsync(new VerifyOtpDto
        {
            UserId = userId,
            Code = "111111",
            Purpose = OtpPurpose.Login   // trying to use registration code as login
        });

        Assert.Null(result);
    }

    // ─── Test 11: Login OTP cannot be used as registration OTP ───────────────

    [Fact]
    public async Task VerifyOtp_LoginOtpCode_CannotBeUsedAsRegistrationOtp()
    {
        var (svc, _, otpSvc, _, _) = BuildService();
        var userId = Guid.NewGuid();
        otpSvc.Setup(o => o.VerifyOtpAsync(userId, "222222", OtpPurpose.Login)).ReturnsAsync(true);
        otpSvc.Setup(o => o.VerifyOtpAsync(userId, "222222", OtpPurpose.Registration)).ReturnsAsync(false);

        var result = await svc.VerifyOtpAsync(new VerifyOtpDto
        {
            UserId = userId,
            Code = "222222",
            Purpose = OtpPurpose.Registration  // trying to use login code as registration
        });

        Assert.Null(result);
    }

    // ─── Test 12: Second login OTP request invalidates the first ──────────────

    [Fact]
    public async Task OtpService_SendOtp_InvalidatesExistingActiveOtps()
    {
        // Tests OtpService directly with a mock repository
        var otpRepo = new Mock<IOtpRepository>();
        var emailSvc = new Mock<Infrastructure.Email.IEmailService>();
        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["OTP_EXPIRATION_MINUTES"]).Returns("10");

        otpRepo.Setup(r => r.InvalidateActiveOtpsAsync(It.IsAny<Guid>(), OtpPurpose.Login))
               .Returns(Task.CompletedTask);
        otpRepo.Setup(r => r.CreateAsync(It.IsAny<OtpVerification>()))
               .Returns(Task.CompletedTask);
        emailSvc.Setup(e => e.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

        var otpService = new OtpService(otpRepo.Object, emailSvc.Object, config.Object);
        var user = ActiveVerifiedUser();

        await otpService.SendOtpAsync(user, OtpPurpose.Login);

        otpRepo.Verify(r => r.InvalidateActiveOtpsAsync(user.Id, OtpPurpose.Login), Times.Once);
    }

    // ─── Test 13: Logout revokes the refresh token ────────────────────────────

    [Fact]
    public async Task RevokeToken_WithValidToken_RevokesStoredToken()
    {
        var (svc, _, _, _, rtRepo) = BuildService();
        var rawToken = "some-raw-refresh-token";
        var tokenHash = SecurityUtils.HashToken(rawToken);
        var stored = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        rtRepo.Setup(r => r.GetByTokenHashAsync(tokenHash)).ReturnsAsync(stored);
        rtRepo.Setup(r => r.UpdateAsync(stored)).Returns(Task.CompletedTask);

        await svc.RevokeTokenAsync(rawToken);

        Assert.NotNull(stored.RevokedAt);
        rtRepo.Verify(r => r.UpdateAsync(stored), Times.Once);
    }

    // ─── Test 14: Logout deletes cookies (controller responsibility — verify service revokes) ─

    [Fact]
    public async Task RevokeToken_AlreadyRevoked_DoesNotDoubleRevoke()
    {
        var (svc, _, _, _, rtRepo) = BuildService();
        var rawToken = "already-revoked-token";
        var tokenHash = SecurityUtils.HashToken(rawToken);
        var stored = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            RevokedAt = DateTime.UtcNow.AddHours(-1) // already revoked
        };
        rtRepo.Setup(r => r.GetByTokenHashAsync(tokenHash)).ReturnsAsync(stored);

        await svc.RevokeTokenAsync(rawToken);

        rtRepo.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    // ─── Test 15: Login after logout requires a new OTP ──────────────────────

    [Fact]
    public async Task Login_AfterLogout_RequiresNewOtp()
    {
        // After a logout the user logs in again — still gets OTP, not tokens
        var (svc, userRepo, otpSvc, _, _) = BuildService();
        var user = ActiveVerifiedUser();
        userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        otpSvc.Setup(o => o.SendOtpAsync(It.IsAny<User>(), OtpPurpose.Login)).Returns(Task.CompletedTask);

        var result = await svc.LoginAsync(new LoginDto(user.Email, "Password123!"));

        Assert.True(result.RequiresOtp);
        Assert.Null(result.AccessToken);
    }

    // ─── Test 16: Registration and account verification still work ────────────

    [Fact]
    public async Task Register_CreatesUserAndSendsRegistrationOtp()
    {
        var (svc, userRepo, otpSvc, _, _) = BuildService();
        userRepo.Setup(r => r.GetByEmailAsync("new@example.com")).ReturnsAsync((User?)null);
        userRepo.Setup(r => r.CreateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        otpSvc.Setup(o => o.SendOtpAsync(It.IsAny<User>(), OtpPurpose.Registration)).Returns(Task.CompletedTask);

        var result = await svc.RegisterAsync(new RegisterDto("Jane", "Doe", "new@example.com", "Password123!"));

        Assert.True(result.RequiresOtp);
        Assert.Null(result.AccessToken);
        otpSvc.Verify(o => o.SendOtpAsync(It.IsAny<User>(), OtpPurpose.Registration), Times.Once);
    }

    [Fact]
    public async Task VerifyOtp_RegistrationPurpose_SetsIsVerifiedAndActivatesAccount()
    {
        var (svc, userRepo, otpSvc, _, _) = BuildService();
        var userId = Guid.NewGuid();
        var user = PendingUnverifiedUser(userId);
        userRepo.Setup(r => r.GetWithRolesAsync(userId)).ReturnsAsync(user);
        userRepo.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);
        otpSvc.Setup(o => o.VerifyOtpAsync(userId, "654321", OtpPurpose.Registration)).ReturnsAsync(true);

        var result = await svc.VerifyOtpAsync(new VerifyOtpDto
        {
            UserId = userId,
            Code = "654321",
            Purpose = OtpPurpose.Registration
        });

        Assert.NotNull(result);
        Assert.True(user.IsVerified);
        Assert.Equal(UserStatus.ACTIVE, user.Status);
    }

    // ─── Test 17: Google OAuth behavior is not broken ─────────────────────────

    [Fact]
    public async Task CompleteGoogleLogin_WithActiveUser_IssuesTokensDirectly()
    {
        var (svc, userRepo, otpSvc, _, _) = BuildService();
        var userId = Guid.NewGuid();
        var user = ActiveVerifiedUser(userId);
        userRepo.Setup(r => r.GetWithRolesAsync(userId)).ReturnsAsync(user);
        userRepo.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

        var result = await svc.CompleteGoogleLoginAsync(userId);

        Assert.NotNull(result);
        Assert.False(result.RequiresOtp);
        Assert.Equal("access-token", result.AccessToken);
        // Google OAuth must never trigger OTP sending
        otpSvc.Verify(o => o.SendOtpAsync(It.IsAny<User>(), It.IsAny<OtpPurpose>()), Times.Never);
    }

    // ─── Test 18: Refresh token rotation still works ──────────────────────────

    [Fact]
    public async Task RefreshToken_WithValidToken_RotatesAndIssuesNewTokens()
    {
        var (svc, userRepo, _, jwtSvc, rtRepo) = BuildService();
        var userId = Guid.NewGuid();
        var user = ActiveVerifiedUser(userId);
        var rawToken = "valid-refresh-token";
        var tokenHash = SecurityUtils.HashToken(rawToken);

        var stored = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(29)
        };

        rtRepo.Setup(r => r.GetByTokenHashAsync(tokenHash)).ReturnsAsync(stored);
        rtRepo.Setup(r => r.UpdateAsync(stored)).Returns(Task.CompletedTask);
        userRepo.Setup(r => r.GetWithRolesAsync(userId)).ReturnsAsync(user);

        var result = await svc.RefreshTokenAsync(rawToken);

        Assert.NotNull(result);
        Assert.False(result!.RequiresOtp);
        Assert.Equal("access-token", result.AccessToken);
        Assert.NotNull(stored.RevokedAt);           // old token revoked
        rtRepo.Verify(r => r.CreateAsync(It.IsAny<RefreshToken>()), Times.Once); // new token stored
    }
}
