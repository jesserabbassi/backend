using NinetyBackend.Modules.Wallets.DTOs;
using NinetyBackend.Modules.Wallets.Models;

namespace NinetyBackend.Modules.Wallets.Services;

public interface ITransactionService
{
    Task<WalletTransactionResponseDto> CreateAsync(Guid customerId, decimal amount, string key, string? reason, bool debit, TransactionType type = TransactionType.CREDIT, CancellationToken cancellationToken = default);
    Task<WalletTransactionResponseDto> RefundAsync(Guid customerId, RefundDto dto, CancellationToken cancellationToken = default);
}
