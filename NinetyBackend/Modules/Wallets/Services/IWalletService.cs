using NinetyBackend.Modules.Wallets.DTOs;

namespace NinetyBackend.Modules.Wallets.Services;

public interface IWalletService
{
    Task<WalletResponseDto> GetOrCreateAsync(Guid customerId);
    Task<List<WalletTransactionResponseDto>> GetTransactionsAsync(Guid customerId);
    Task<WalletTransactionResponseDto> CreditAsync(Guid customerId, WalletOperationDto dto);
    Task<WalletTransactionResponseDto> DebitAsync(Guid customerId, WalletOperationDto dto);
}
