using NinetyBackend.Modules.Wallets.Models;

namespace NinetyBackend.Modules.Wallets.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByCustomerIdAsync(Guid customerId);
    Task<Wallet?> GetByIdAsync(Guid id);
    Task<Wallet?> GetByCustomerIdForUpdateAsync(Guid customerId);
    Task<WalletTransaction?> GetTransactionByIdempotencyKeyAsync(Guid walletId, string key);
    Task<WalletTransaction?> GetTransactionAsync(Guid id, Guid walletId);
    Task<List<WalletTransaction>> GetTransactionsAsync(Guid walletId);
    Task AddAsync(Wallet wallet);
    Task AddTransactionAsync(WalletTransaction transaction);
    Task SaveChangesAsync();
}
