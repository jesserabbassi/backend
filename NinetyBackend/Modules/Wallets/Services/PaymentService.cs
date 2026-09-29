using NinetyBackend.Modules.Wallets.DTOs;
using NinetyBackend.Modules.Wallets.Models;
using NinetyBackend.Modules.Wallets.Repositories;

namespace NinetyBackend.Modules.Wallets.Services;

public interface IPaymentService
{
    Task<WalletTransactionResponseDto> ProcessAsync(Guid customerId, PaymentDto dto, CancellationToken cancellationToken = default);
    Task<WalletTransactionResponseDto> RefundAsync(Guid customerId, RefundDto dto, CancellationToken cancellationToken = default);
}

public class PaymentService(ITransactionService transactions, IWalletRepository wallets) : IPaymentService
{
    public async Task<WalletTransactionResponseDto> ProcessAsync(Guid customerId, PaymentDto dto, CancellationToken cancellationToken = default)
    {
        var wallet = await wallets.GetByCustomerIdAsync(customerId) ?? throw new KeyNotFoundException("Wallet not found.");
        if (dto.WalletId != wallet.Id) throw new UnauthorizedAccessException("Wallet does not belong to the authenticated user.");
        return await transactions.CreateAsync(customerId, dto.Amount, dto.IdempotencyKey, "Payment", false, TransactionType.PAYMENT, cancellationToken);
    }
    public Task<WalletTransactionResponseDto> RefundAsync(Guid customerId, RefundDto dto, CancellationToken cancellationToken = default) => transactions.RefundAsync(customerId, dto, cancellationToken);
}
