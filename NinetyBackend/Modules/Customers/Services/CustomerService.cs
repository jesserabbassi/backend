using NinetyBackend.Modules.Customers.DTOs;
using NinetyBackend.Modules.Customers.Models;
using NinetyBackend.Modules.Customers.Repositories;

namespace NinetyBackend.Modules.Customers.Services;

public interface ICustomerService
{
    Task<IEnumerable<CustomerResponseDto>> GetAllAsync();
    Task<CustomerResponseDto?> GetByIdAsync(Guid id);
    Task<CustomerResponseDto> CreateAsync(CreateCustomerDto dto);
    Task<CustomerResponseDto?> UpdateAsync(Guid id, UpdateCustomerDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class CustomerService(ICustomerRepository customerRepository) : ICustomerService
{
    public async Task<IEnumerable<CustomerResponseDto>> GetAllAsync()
    {
        var customers = await customerRepository.GetAllAsync();
        return customers.Select(MapToResponseDto);
    }

    public async Task<CustomerResponseDto?> GetByIdAsync(Guid id)
    {
        var customer = await customerRepository.GetByIdAsync(id);
        return customer is null ? null : MapToResponseDto(customer);
    }

    public async Task<CustomerResponseDto> CreateAsync(CreateCustomerDto dto)
    {
        ValidateRequiredFields(
            dto.FirstName,
            dto.LastName,
            dto.Phone,
            "Customer first name, last name, phone, and email are required.",
            dto.Email);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Phone = dto.Phone.Trim(),
            Email = dto.Email.Trim(),
            Status = CustomerStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow
        };

        await customerRepository.AddAsync(customer);
        return MapToResponseDto(customer);
    }

    public async Task<CustomerResponseDto?> UpdateAsync(Guid id, UpdateCustomerDto dto)
    {
        var customer = await customerRepository.GetByIdAsync(id);
        if (customer is null)
        {
            return null;
        }

        ValidateRequiredFields(dto.FirstName, dto.LastName, dto.Phone, "Customer first name, last name, and phone are required.");

        customer.FirstName = dto.FirstName.Trim();
        customer.LastName = dto.LastName.Trim();
        customer.Phone = dto.Phone.Trim();

        await customerRepository.UpdateAsync(customer);
        return MapToResponseDto(customer);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var customer = await customerRepository.GetByIdAsync(id);
        if (customer is null)
        {
            return false;
        }

        await customerRepository.DeleteAsync(customer);
        return true;
    }

    private static void ValidateRequiredFields(string? firstName, string? lastName, string? phone, string message, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(firstName)
            || string.IsNullOrWhiteSpace(lastName)
            || string.IsNullOrWhiteSpace(phone)
            || (email is not null && string.IsNullOrWhiteSpace(email)))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static CustomerResponseDto MapToResponseDto(Customer customer)
    {
        return new CustomerResponseDto(
            customer.Id,
            customer.UserId,
            customer.FirstName,
            customer.LastName,
            customer.Phone,
            customer.Email,
            customer.Status,
            customer.CreatedAt);
    }
}
