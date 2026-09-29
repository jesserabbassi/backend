using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.Stations.DTOs;
using NinetyBackend.Modules.Stations.Services;
using NinetyBackend.Modules.Auth.Services;

namespace NinetyBackend.Modules.Stations.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Supervisor")]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IJwtService _jwtService;

    public AgentController(IAgentService agentService, IJwtService jwtService)
    {
        _agentService = agentService;
        _jwtService = jwtService;
    }

    [HttpPost("{id:guid}/token")]
    public async Task<IActionResult> IssueToken(Guid id)
    {
        var agent = await _agentService.GetByIdAsync(id);
        if (agent is null)
            return NotFound(new { message = "Agent not found." });

        return Ok(new
        {
            agentId = agent.Id,
            stationId = agent.StationId,
            accessToken = _jwtService.GenerateAgentAccessToken(agent.Id)
        });
    }

    [HttpPost("register")]
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

    [HttpPost("{id:guid}/heartbeat")]
    public async Task<IActionResult> Heartbeat(Guid id)
    {
        var result = await _agentService.HeartbeatAsync(id);
        if (result == null)
        {
            return NotFound(new { message = "Agent not found." });
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/disconnect")]
    public async Task<IActionResult> Disconnect(Guid id)
    {
        var success = await _agentService.DisconnectAsync(id);
        if (!success)
        {
            return NotFound(new { message = "Agent not found." });
        }

        return Ok(new { message = "Agent disconnected successfully." });
    }
}
