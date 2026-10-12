using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.Stations.DTOs;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Stations.Services;

namespace NinetyBackend.Modules.Stations.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;

    public AgentController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    /// <summary>
    /// Issue access + refresh tokens for an Agent. Admin/Supervisor provisioning flow.
    /// </summary>
    [HttpPost("{id:guid}/token")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> IssueToken(Guid id)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _agentService.IssueTokensAsync(id, ip);
            return Accepted(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("pending")][Authorize(Roles="Admin,Supervisor")]
    public async Task<IActionResult> Pending() => Ok(await _agentService.GetPendingAsync());
    [HttpPost("{id:guid}/approve")][Authorize(Roles="Admin,Supervisor")]
    public async Task<IActionResult> Approve(Guid id) => (await _agentService.ApproveAsync(id)) is { } a ? Ok(a) : NotFound();
    [HttpPost("{id:guid}/reject")][Authorize(Roles="Admin,Supervisor")]
    public async Task<IActionResult> Reject(Guid id) => (await _agentService.RejectAsync(id)) is { } a ? Ok(a) : NotFound();
    [HttpPost("{id:guid}/revoke")][Authorize(Roles="Admin,Supervisor")]
    public async Task<IActionResult> Revoke(Guid id) => (await _agentService.RevokeAsync(id)) is { } a ? Ok(a) : NotFound();
    [HttpPost("{id:guid}/bootstrap")][AllowAnonymous]
    public async Task<IActionResult> Bootstrap(Guid id, [FromBody] AgentBootstrapDto dto)
    {
        var agent = await _agentService.GetByIdAsync(id);
        if (agent is null)
            return NotFound(new { message = "Agent registration not found." });
        if (agent.Status == nameof(AgentStatus.Pending))
            return Unauthorized(new { status = "pending" });
        if (agent.Status == nameof(AgentStatus.Rejected))
            return Conflict(new { status = "rejected", message = "Supervisor rejected this registration." });
        if (agent.Status == nameof(AgentStatus.Revoked))
            return StatusCode(StatusCodes.Status403Forbidden, new { status = "revoked" });

        var tokens = await _agentService.ExchangeBootstrapAsync(id, dto.BootstrapSecret, HttpContext.Connection.RemoteIpAddress?.ToString());
        return tokens is null ? Unauthorized(new { message = "Bootstrap credential rejected." }) : Ok(tokens);
    }

    /// <summary>
    /// Refresh an Agent access token using a valid refresh token.
    /// No prior auth required — the refresh token IS the credential.
    /// </summary>
    [HttpPost("token/refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] AgentRefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest(new { message = "Refresh token is required." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _agentService.RefreshTokenAsync(dto.RefreshToken, ip);

        if (result is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        return Ok(result);
    }

    /// <summary>
    /// Revoke one Agent refresh token during logout or local decommissioning.
    /// </summary>
    [HttpPost("token/revoke")]
    [AllowAnonymous]
    public async Task<IActionResult> RevokeRefreshToken([FromBody] AgentRefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest(new { message = "Refresh token is required." });

        await _agentService.RevokeRefreshTokenAsync(dto.RefreshToken);
        return NoContent();
    }

    /// <summary>
    /// Register (or re-register) an Agent for a Station. Admin/Supervisor only.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterAgentDto dto)
    {
        try
        {
            var result = await _agentService.RegisterAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get Agent by id. Admin/Supervisor only.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var agent = await _agentService.GetByIdAsync(id);
        if (agent is null)
            return NotFound(new { message = "Agent not found." });

        return Ok(agent);
    }

    /// <summary>
    /// Get Agent connection status. Admin/Supervisor only.
    /// </summary>
    [HttpGet("{id:guid}/status")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetStatus(Guid id)
    {
        var agent = await _agentService.GetByIdAsync(id);
        if (agent is null)
            return NotFound(new { message = "Agent not found." });

        return Ok(new { agentId = agent.Id, status = agent.Status, lastHeartbeatAt = agent.LastHeartbeatAt });
    }

    /// <summary>
    /// Heartbeat — authenticated Agent only (agent_id claim required).
    /// </summary>
    [HttpPost("{id:guid}/heartbeat")]
    [Authorize]
    public async Task<IActionResult> Heartbeat(Guid id)
    {
        // Agent can only heartbeat itself
        if (!IsCallerAgent(id) && !User.IsInRole("Admin") && !User.IsInRole("Supervisor"))
            return Forbid();

        var result = await _agentService.HeartbeatAsync(id);
        if (result == null)
            return NotFound(new { message = "Agent not found." });

        return Ok(result);
    }

    /// <summary>
    /// Disconnect — authenticated Agent only (agent_id claim required).
    /// </summary>
    [HttpPost("{id:guid}/disconnect")]
    [Authorize]
    public async Task<IActionResult> Disconnect(Guid id)
    {
        // Agent can only disconnect itself
        if (!IsCallerAgent(id) && !User.IsInRole("Admin") && !User.IsInRole("Supervisor"))
            return Forbid();

        var success = await _agentService.DisconnectAsync(id);
        if (!success)
            return NotFound(new { message = "Agent not found." });

        return Ok(new { message = "Agent disconnected successfully." });
    }

    /// <summary>Returns true if the JWT caller is this specific agent.</summary>
    private bool IsCallerAgent(Guid agentId)
    {
        var claim = User.FindFirstValue("agent_id");
        return Guid.TryParse(claim, out var id) && id == agentId;
    }
}
