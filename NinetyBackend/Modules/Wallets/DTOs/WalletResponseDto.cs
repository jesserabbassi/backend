namespace NinetyBackend.Modules.Wallets.DTOs;

public record WalletResponseDto(Guid Id, Guid CustomerId, decimal Balance, string Currency, long Version);
public record WalletTransactionResponseDto(Guid Id, Guid WalletId, string Type, decimal Amount, decimal PreviousBalance, decimal NewBalance, string? Reason, string? IdempotencyKey, DateTime CreatedAt);
