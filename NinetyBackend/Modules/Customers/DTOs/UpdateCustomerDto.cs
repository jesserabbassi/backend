namespace NinetyBackend.Modules.Customers.DTOs;

public record UpdateCustomerDto(
    string FirstName,
    string LastName,
    string Phone
);
