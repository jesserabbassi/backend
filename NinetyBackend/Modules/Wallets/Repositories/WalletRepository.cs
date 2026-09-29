using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.Wallets.Models;

namespace NinetyBackend.Modules.Wallets.Repositories;

public class WalletRepository(ApplicationDbContext db) : IWalletRepository
{
    public Task<Wallet?> GetByCustomerIdAsync(Guid customerId) => db.Wallets.SingleOrDefaultAsync(x => x.CustomerId == customerId);
    public Task<Wallet?> GetByIdAsync(Guid id) => db.Wallets.SingleOrDefaultAsync(x => x.Id == id);
    public Task<Wallet?> GetByCustomerIdForUpdateAsync(Guid customerId) => db.Wallets.FromSqlInterpolated($"SELECT * FROM \"Wallets\" WHERE \"CustomerId\" = {customerId} FOR UPDATE").SingleOrDefaultAsync();
    public Task<WalletTransaction?> GetTransactionByIdempotencyKeyAsync(Guid walletId, string key) => db.WalletTransactions.SingleOrDefaultAsync(x => x.WalletId == walletId && x.IdempotencyKey == key);
    public Task<WalletTransaction?> GetTransactionAsync(Guid id, Guid walletId) => db.WalletTransactions.SingleOrDefaultAsync(x => x.Id == id && x.WalletId == walletId);
    public Task<List<WalletTransaction>> GetTransactionsAsync(Guid walletId) => db.WalletTransactions.Where(x => x.WalletId == walletId).OrderByDescending(x => x.CreatedAt).ToListAsync();
    public Task AddAsync(Wallet wallet) => db.Wallets.AddAsync(wallet).AsTask();
    public Task AddTransactionAsync(WalletTransaction transaction) => db.WalletTransactions.AddAsync(transaction).AsTask();
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
