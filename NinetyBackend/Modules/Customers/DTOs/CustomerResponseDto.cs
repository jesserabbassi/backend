using NinetyBackend.Modules.Customers.Models;

namespace NinetyBackend.Modules.Customers.DTOs;

public record CustomerResponseDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    CustomerStatus Status,
    DateTime CreatedAt
);
