namespace NinetyBackend.Modules.Wallets.DTOs;

public class WalletOperationDto
{
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
