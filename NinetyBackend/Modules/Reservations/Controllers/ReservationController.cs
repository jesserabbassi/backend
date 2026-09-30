using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.Reservations.DTOs;
using NinetyBackend.Modules.Reservations.Services;

namespace NinetyBackend.Modules.Reservations.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationController(IReservationService reservationService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto request)
    {
        try
        {
            var reservation = await reservationService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, reservation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var reservation = await reservationService.GetByIdAsync(id);
        return reservation == null
            ? NotFound(new { message = "Reservation not found." })
            : Ok(reservation);
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomer(Guid customerId)
    {
        return Ok(await reservationService.GetByCustomerAsync(customerId));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            return await reservationService.CancelAsync(id)
                ? Ok(new { message = "Reservation cancelled." })
                : NotFound(new { message = "Reservation not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        try
        {
            var reservation = await reservationService.ConfirmAsync(id);
            return reservation == null
                ? NotFound(new { message = "Reservation not found." })
                : Ok(reservation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("check-availability")]
    public async Task<IActionResult> CheckAvailability([FromBody] AvailabilityRequestDto request)
    {
        try
        {
            return Ok(await reservationService.CheckAvailabilityAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}