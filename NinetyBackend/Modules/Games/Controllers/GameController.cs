using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.Games.DTOs;
using NinetyBackend.Modules.Games.Services;

namespace NinetyBackend.Modules.Games.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController(IGameCatalogService gameCatalogService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await gameCatalogService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var game = await gameCatalogService.GetByIdAsync(id);
        return game is null
            ? NotFound(new { message = "Game not found." })
            : Ok(game);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGameDto dto)
    {
        try
        {
            var game = await gameCatalogService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = game.Id }, game);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGameDto dto)
    {
        try
        {
            var game = await gameCatalogService.UpdateAsync(id, dto);
            return game is null
                ? NotFound(new { message = "Game not found." })
                : Ok(game);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await gameCatalogService.DeleteAsync(id);
        return deleted
            ? Ok(new { message = "Game deactivated successfully." })
            : NotFound(new { message = "Game not found." });
    }
}