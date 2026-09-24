using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.Stations.DTOs;
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
