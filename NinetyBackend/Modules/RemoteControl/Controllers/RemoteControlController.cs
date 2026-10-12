using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.RemoteControl.Models;
using NinetyBackend.Modules.RemoteControl.Services;
using NinetyBackend.Modules.Stations.Services;

namespace NinetyBackend.Modules.RemoteControl.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Supervisor")]
[Route("api/remote-control")]
public class RemoteControlController(
    IRemoteControlService remoteControlService,
    IAgentService agentService) : ControllerBase
{
    [HttpPost("stations/{stationId:guid}/commands/{type}")]
    public async Task<IActionResult> SendCommand(Guid stationId, CommandType type, [FromBody] object? payload, CancellationToken ct)
    {
        try
        {
            var result = await remoteControlService.SendCommandAsync(stationId, type, payload, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("stations/{stationId:guid}/commands")]
    public async Task<IActionResult> GetHistory(Guid stationId, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var commands = await remoteControlService.GetHistoryAsync(stationId, take, ct);
        return Ok(commands);
    }

    [HttpGet("commands/{commandId:guid}")]
    public async Task<IActionResult> GetById(Guid commandId, CancellationToken ct)
    {
        var command = await remoteControlService.GetByIdAsync(commandId, ct);
        return command is null ? NotFound(new { message = "Command not found." }) : Ok(command);
    }

    /// <summary>
    /// Acknowledge a command result. Called by the Rust Agent.
    /// Admin/Supervisor OR the Agent assigned to the station's command.
    /// </summary>
    [HttpPost("commands/{commandId:guid}/ack")]
    [Authorize] // allow agents too; we validate identity below
    public async Task<IActionResult> Acknowledge(Guid commandId, [FromBody] CommandAcknowledgementDto dto, CancellationToken ct)
    {
        // If not admin/supervisor, must be the assigned agent
        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor"))
        {
            var command = await remoteControlService.GetByIdAsync(commandId, ct);
            if (command is null)
                return NotFound(new { message = "Command not found." });

            if (!await IsAssignedAgentAsync(command.StationId))
                return Forbid();
        }

        var result = await remoteControlService.AcknowledgeAsync(commandId, dto.Success, dto.Message, ct);
        return result is null ? NotFound(new { message = "Command not found." }) : Ok(result);
    }

    private async Task<bool> IsAssignedAgentAsync(Guid stationId)
    {
        var claim = User.FindFirstValue("agent_id");
        return Guid.TryParse(claim, out var agentId)
            && (await agentService.GetByIdAsync(agentId))?.StationId == stationId;
    }
}
