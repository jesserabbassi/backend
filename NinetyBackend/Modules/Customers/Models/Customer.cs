namespace NinetyBackend.Modules.Customers.Models;

public class Customer
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CustomerStatus Status { get; set; } = CustomerStatus.ACTIVE;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum CustomerStatus
{
    ACTIVE,
    BLOCKED,
    SUSPENDED
}
