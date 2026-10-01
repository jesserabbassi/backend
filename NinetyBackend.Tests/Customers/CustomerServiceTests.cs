using Moq;
using NinetyBackend.Modules.Customers.DTOs;
using NinetyBackend.Modules.Customers.Models;
using NinetyBackend.Modules.Customers.Repositories;
using NinetyBackend.Modules.Customers.Services;

namespace NinetyBackend.Tests.Customers;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _service = new CustomerService(_customerRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesActiveCustomer()
    {
        _customerRepository.Setup(r => r.AddAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(new CreateCustomerDto(" Jane ", " Doe ", " 555-0100 ", " jane@example.com "));

        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("555-0100", result.Phone);
        Assert.Equal("jane@example.com", result.Email);
        Assert.Equal(CustomerStatus.ACTIVE, result.Status);
        _customerRepository.Verify(r => r.AddAsync(It.Is<Customer>(c => c.FirstName == "Jane")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCustomerExists_UpdatesContactFields()
    {
        var id = Guid.NewGuid();
        var customer = new Customer { Id = id, FirstName = "Jane", LastName = "Doe", Phone = "555-0100", Email = "jane@example.com" };
        _customerRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(customer);
        _customerRepository.Setup(r => r.UpdateAsync(customer)).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(id, new UpdateCustomerDto("Janet", "Doe", "555-0101"));

        Assert.NotNull(result);
        Assert.Equal("Janet", result.FirstName);
        Assert.Equal("555-0101", result.Phone);
        Assert.Equal("jane@example.com", result.Email);
        _customerRepository.Verify(r => r.UpdateAsync(customer), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCustomerDoesNotExist_ReturnsFalse()
    {
        _customerRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Customer?)null);

        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
        _customerRepository.Verify(r => r.DeleteAsync(It.IsAny<Customer>()), Times.Never);
    }
}
