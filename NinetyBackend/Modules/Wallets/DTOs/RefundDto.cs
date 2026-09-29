namespace NinetyBackend.Modules.Wallets.DTOs;

public class RefundDto
{
    public Guid TransactionId { get; set; }
    public string? Reason { get; set; }
}
