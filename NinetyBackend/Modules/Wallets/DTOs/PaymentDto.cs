namespace NinetyBackend.Modules.Wallets.DTOs;

public class PaymentDto
{
    public decimal Amount { get; set; }
    public Guid WalletId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
