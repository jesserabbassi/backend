using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.Stations.DTOs;
using NinetyBackend.Modules.Stations.Services;

namespace NinetyBackend.Modules.Stations.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Supervisor")]
[Route("api/[controller]")]
public class StationController : ControllerBase
{
    private readonly IStationService _stationService;

    public StationController(IStationService stationService)
    {
        _stationService = stationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var stations = await _stationService.GetAllAsync();
        return Ok(stations);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var station = await _stationService.GetByIdAsync(id);
        if (station == null)
        {
            return NotFound(new { message = "Station not found." });
        }

        return Ok(station);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStationDto dto)
    {
        try
        {
            var result = await _stationService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStationDto dto)
    {
        try
        {
            var result = await _stationService.UpdateAsync(id, dto);
            if (result == null)
            {
                return NotFound(new { message = "Station not found." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _stationService.DeleteAsync(id);
        if (!success)
        {
            return NotFound(new { message = "Station not found." });
        }

        return Ok(new { message = "Station deleted successfully." });
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid id)
    {
        var status = await _stationService.GetStatusAsync(id);
        if (status == null)
        {
            return NotFound(new { message = "Station not found." });
        }

        return Ok(new { status });
    }
}
