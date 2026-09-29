using System.Data;
using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.Wallets.DTOs;
using NinetyBackend.Modules.Wallets.Models;
using NinetyBackend.Modules.Wallets.Repositories;

namespace NinetyBackend.Modules.Wallets.Services;

public class TransactionService(ApplicationDbContext db, IWalletRepository wallets) : ITransactionService
{
    public async Task<WalletTransactionResponseDto> CreateAsync(Guid customerId, decimal amount, string key, string? reason, bool debit, TransactionType type = TransactionType.CREDIT, CancellationToken cancellationToken = default)
    {
        Validate(amount, key);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var wallet = await wallets.GetByCustomerIdForUpdateAsync(customerId) ?? throw new KeyNotFoundException("Wallet not found.");
        var existing = await wallets.GetTransactionByIdempotencyKeyAsync(wallet.Id, key);
        if (existing is not null) { await tx.CommitAsync(cancellationToken); return WalletService.Map(existing); }
        if (debit && wallet.Balance < amount) throw new InvalidOperationException("Insufficient wallet balance.");
        var previous = wallet.Balance;
        wallet.Balance = debit ? previous - amount : previous + amount;
        wallet.Version++;
        var transaction = new WalletTransaction { Id = Guid.NewGuid(), WalletId = wallet.Id, Type = debit ? TransactionType.DEBIT : type, Amount = amount, PreviousBalance = previous, NewBalance = wallet.Balance, IdempotencyKey = key, Reason = reason };
        await wallets.AddTransactionAsync(transaction);
        await wallets.SaveChangesAsync();
        await tx.CommitAsync(cancellationToken);
        return WalletService.Map(transaction);
    }

    public async Task<WalletTransactionResponseDto> RefundAsync(Guid customerId, RefundDto dto, CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var wallet = await wallets.GetByCustomerIdForUpdateAsync(customerId) ?? throw new KeyNotFoundException("Wallet not found.");
        var original = await wallets.GetTransactionAsync(dto.TransactionId, wallet.Id) ?? throw new KeyNotFoundException("Transaction not found.");
        if (original.Type != TransactionType.PAYMENT && original.Type != TransactionType.CREDIT) throw new InvalidOperationException("Transaction cannot be refunded.");
        var previous = wallet.Balance;
        wallet.Balance += original.Amount;
        wallet.Version++;
        var refund = new WalletTransaction { Id = Guid.NewGuid(), WalletId = wallet.Id, Type = TransactionType.REFUND, Amount = original.Amount, PreviousBalance = previous, NewBalance = wallet.Balance, Reason = dto.Reason };
        await wallets.AddTransactionAsync(refund); await wallets.SaveChangesAsync(); await tx.CommitAsync(cancellationToken);
        return WalletService.Map(refund);
    }

    private static void Validate(decimal amount, string key) { if (amount <= 0 || decimal.Round(amount, 2) != amount) throw new ArgumentException("Amount must be positive with at most two decimal places."); if (string.IsNullOrWhiteSpace(key) || key.Length > 128) throw new ArgumentException("A valid idempotency key is required."); }
}
