using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinetyGamingStationBackend.Application.DTOs.RemoteControl;
using NinetyGamingStationBackend.Application.Interfaces;
using NinetyGamingStationBackend.Domain.Enums;

namespace NinetyGamingStationBackend.Controllers;

public sealed record CommandPayload(string? SessionId, string? Reason, string? Executable);

[ApiController]
[Route("api/stations/{stationId:guid}/commands")]
[Authorize(Policy = "StationOperators")]
public sealed class RemoteControlController(IRemoteControlService service, IStationRepository stations, IBranchAccessService access) : ControllerBase
{
    [HttpPost("{type}")]
    public async Task<ActionResult<CommandResultDto>> Execute(Guid stationId, CommandType type, CommandPayload? payload, CancellationToken ct)
    {
        try
        {
            var station = await stations.GetByIdAsync(stationId, ct);
            if (station is null) return NotFound(new { error = "station_not_found" });
            if (!access.CanAccess(User, station.BranchId)) return Forbid();
            var result = await service.ExecuteAsync(stationId, type, payload, ct);
            if (result is null) return NotFound(new { error = "station_not_found" });
            return AcceptedAtAction(nameof(GetCommand), new { stationId, commandId = result.CommandId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{commandId:guid}", Name = nameof(GetCommand))]
    public async Task<ActionResult<CommandResultDto>> GetCommand(Guid stationId, Guid commandId, CancellationToken ct)
    {
        var station = await stations.GetByIdAsync(stationId, ct);
        if (station is null || !access.CanAccess(User, station.BranchId)) return NotFound(new { error = "command_not_found" });
        var result = await service.GetCommandAsync(commandId, ct);
        return result is null || result.StationId != stationId
            ? NotFound(new { error = "command_not_found" })
            : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CommandResultDto>>> History(Guid stationId, [FromQuery] int take = 100, CancellationToken ct = default)
        {
        var station = await stations.GetByIdAsync(stationId, ct);
        if (station is null) return NotFound(new { error = "station_not_found" });
        if (!access.CanAccess(User, station.BranchId)) return Forbid();
        return Ok(await service.GetHistoryAsync(stationId, take, ct));
    }
}
