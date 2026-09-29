using Microsoft.EntityFrameworkCore;
using NinetyBackend.Modules.Wallets.DTOs;
using NinetyBackend.Modules.Wallets.Models;
using NinetyBackend.Modules.Wallets.Repositories;

namespace NinetyBackend.Modules.Wallets.Services;

public class WalletService(IWalletRepository wallets) : IWalletService
{
    public async Task<WalletResponseDto> GetOrCreateAsync(Guid customerId)
    {
        var wallet = await wallets.GetByCustomerIdAsync(customerId);
        if (wallet is null)
        {
            wallet = new Wallet { Id = Guid.NewGuid(), CustomerId = customerId };
            await wallets.AddAsync(wallet);
            try { await wallets.SaveChangesAsync(); }
            catch (DbUpdateException) { wallet = await wallets.GetByCustomerIdAsync(customerId) ?? throw new InvalidOperationException("Unable to create wallet."); }
        }
        return Map(wallet);
    }

    public async Task<List<WalletTransactionResponseDto>> GetTransactionsAsync(Guid customerId)
    {
        var wallet = await wallets.GetByCustomerIdAsync(customerId) ?? throw new KeyNotFoundException("Wallet not found.");
        return (await wallets.GetTransactionsAsync(wallet.Id)).Select(Map).ToList();
    }

    public Task<WalletTransactionResponseDto> CreditAsync(Guid customerId, WalletOperationDto dto) => throw new NotSupportedException("Use transaction service.");
    public Task<WalletTransactionResponseDto> DebitAsync(Guid customerId, WalletOperationDto dto) => throw new NotSupportedException("Use transaction service.");
    internal static WalletResponseDto Map(Wallet x) => new(x.Id, x.CustomerId, x.Balance, x.Currency, x.Version);
    internal static WalletTransactionResponseDto Map(WalletTransaction x) => new(x.Id, x.WalletId, x.Type.ToString(), x.Amount, x.PreviousBalance, x.NewBalance, x.Reason, x.IdempotencyKey, x.CreatedAt);
}
