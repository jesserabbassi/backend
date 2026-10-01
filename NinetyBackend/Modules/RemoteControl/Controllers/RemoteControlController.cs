using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.RemoteControl.Models;
using NinetyBackend.Modules.RemoteControl.Services;

namespace NinetyBackend.Modules.RemoteControl.Controllers;

[ApiController]
[Route("api/remote-control")]
public class RemoteControlController(IRemoteControlService remoteControlService) : ControllerBase
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

    [HttpPost("commands/{commandId:guid}/ack")]
    public async Task<IActionResult> Acknowledge(Guid commandId, [FromBody] CommandAcknowledgementDto dto, CancellationToken ct)
    {
        var result = await remoteControlService.AcknowledgeAsync(commandId, dto.Success, dto.Message, ct);
        return result is null ? NotFound(new { message = "Command not found." }) : Ok(result);
    }
}
