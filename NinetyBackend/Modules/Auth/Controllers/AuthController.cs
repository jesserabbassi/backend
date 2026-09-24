using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.Auth.DTOs;
using NinetyBackend.Modules.Auth.Services;

namespace NinetyBackend.Modules.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IOAuthService _oauthService;

    public AuthController(IAuthService authService, IOAuthService oauthService)
    {
        _authService = authService;
        _oauthService = oauthService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.LoginAsync(dto, ip);

            if (!result.RequiresOtp && !string.IsNullOrEmpty(result.AccessToken))
            {
                SetTokenCookies(result.AccessToken, result.RefreshToken);
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.VerifyOtpAsync(dto, ip);

        if (result == null)
        {
            return BadRequest(new { message = "Invalid or expired OTP code." });
        }

        if (!string.IsNullOrEmpty(result.AccessToken))
        {
            SetTokenCookies(result.AccessToken, result.RefreshToken);
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto? dto)
    {
        var token = dto?.Token ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshTokenAsync(token, ip);

        if (result == null)
        {
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        SetTokenCookies(result.AccessToken, result.RefreshToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.RevokeTokenAsync(refreshToken);
        }

        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback))
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded)
        {
            return BadRequest(new { message = "Google authentication failed." });
        }

        var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
        var providerKey = authenticateResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var firstName = authenticateResult.Principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = authenticateResult.Principal.FindFirstValue(ClaimTypes.Surname);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerKey))
        {
            return BadRequest(new { message = "Failed to retrieve user information from Google." });
        }

        var user = await _oauthService.ProcessGoogleLoginAsync(email, providerKey, firstName, lastName);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var authResult = await _authService.CompleteGoogleLoginAsync(user.Id, ip);
            SetTokenCookies(authResult.AccessToken, authResult.RefreshToken);
            return Ok(authResult);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }

    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var profile = await _authService.GetUserProfileAsync(userId);
        if (profile == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            profile.Id,
            profile.Email,
            profile.IsVerified,
            profile.Status,
            profile.CreatedAt,
            profile.LastLoginAt,
            Roles = profile.Roles?.Select(r => r.Name)
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var success = await _authService.ChangePasswordAsync(userId, dto);
        if (!success)
        {
            return BadRequest(new { message = "Password change failed. Verify current password." });
        }

        return Ok(new { message = "Password changed successfully." });
    }

    private void SetTokenCookies(string? accessToken, string? refreshToken)
    {
        var isHttps = Request.IsHttps;

        if (!string.IsNullOrEmpty(accessToken))
        {
            Response.Cookies.Append("accessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });
        }

        if (!string.IsNullOrEmpty(refreshToken))
        {
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }
    }
}
