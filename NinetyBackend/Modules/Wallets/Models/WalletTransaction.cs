namespace NinetyBackend.Modules.Wallets.Models;

public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal NewBalance { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Wallet? Wallet { get; set; }
}

public enum TransactionType { CREDIT, DEBIT, PAYMENT, REFUND, ADJUSTMENT }
