namespace NinetyBackend.Modules.Customers.DTOs;

public record CreateCustomerDto(
    string FirstName,
    string LastName,
    string Phone,
    string Email
);
