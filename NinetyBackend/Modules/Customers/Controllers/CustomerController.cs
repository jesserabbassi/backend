using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.Customers.DTOs;
using NinetyBackend.Modules.Customers.Services;

namespace NinetyBackend.Modules.Customers.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await customerService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var customer = await customerService.GetByIdAsync(id);
        return customer is null
            ? NotFound(new { message = "Customer not found." })
            : Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        try
        {
            var customer = await customerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerDto dto)
    {
        try
        {
            var customer = await customerService.UpdateAsync(id, dto);
            return customer is null
                ? NotFound(new { message = "Customer not found." })
                : Ok(customer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await customerService.DeleteAsync(id);
        return deleted
            ? Ok(new { message = "Customer deleted successfully." })
            : NotFound(new { message = "Customer not found." });
    }
}
