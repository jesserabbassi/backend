using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Wallets.Models;

public class Wallet
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "TND";
    public long Version { get; set; }
    public User? Customer { get; set; }
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
